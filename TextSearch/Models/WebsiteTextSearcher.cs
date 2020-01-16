using HtmlAgilityPack;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using TextSearch.Helpers;
using TextSearch.Model;

namespace TextSearch
{
    [DebuggerDisplay("{SearchState}")]
    public class WebsiteTextSearcher : ITextSearcher
    {
        private readonly CancellationTokenSource _cancelTokenSource;
        private readonly CancellationToken _token;
        private ManualResetEvent _manualResetEvent = new ManualResetEvent(false);

        private readonly string _searchedText;

        private SizeLimitedDictionary<string, ITextSearcher> _visitedLinks;

        public SearchState SearchState { get; set; } = SearchState.Unknown;

        public string SearchPath { get; set; }

        public string ErrorText { get; private set; }

        public WebsiteTextSearcher(string websiteUrl, SizeLimitedDictionary<string, ITextSearcher> visitedLinks, string searchText)
        {
            SearchPath = websiteUrl;
            _visitedLinks = visitedLinks;
            _searchedText = searchText;

            _cancelTokenSource = new CancellationTokenSource();
            _token = _cancelTokenSource.Token;
        }

        public event EventHandler OnSearcherUpdate;

        public async Task<List<string>> StartAsync()
        {
            var website = await Download(SearchPath);
            return website != null && !_cancelTokenSource.IsCancellationRequested ? Parse(website) : new List<string>();
        }

        public void PauseResume()
        {
            switch (SearchState)
            {
                case SearchState.Download:
                case SearchState.Scan:
                    TryUpdateScanState(SearchState.Paused);
                    _manualResetEvent.Reset();
                    break;

                case SearchState.Paused:
                    TryUpdateScanState(SearchState.Scan);
                    _manualResetEvent.Set();
                    _manualResetEvent = new ManualResetEvent(false);
                    break;
            }
        }

        public void Stop()
        {
            if (TryUpdateScanState(SearchState.Canceled))
            {
                _cancelTokenSource.Cancel();
            }
        }

        async Task<HtmlDocument> Download(string url)
        {
            HtmlDocument doc = null;

            using (var client = new MyWebClient())
            {
                try
                {
                    TryUpdateScanState(SearchState.Download);
                    var data = await client.DownloadStringTaskAsync(new Uri(url));
                    doc = new HtmlDocument();
                    doc.LoadHtml(data);

                    if(SearchState == SearchState.Paused)
                    {
                        _manualResetEvent.WaitOne();
                    }
                }
                catch (Exception ex)
                {
                    ErrorText = ex.Message;
                    TryUpdateScanState(SearchState.Error);
                }
            }

            return doc;
        }

        List<string> Parse(HtmlDocument doc)
        {
            var foundUrl = new List<string>();
            var nodes = new Queue<HtmlNode>();
            bool isTextFound = false;

            nodes.Enqueue(doc.DocumentNode);

            TryUpdateScanState(SearchState.Scan);
            
            while (nodes.Count != 0)
            {
                if(_cancelTokenSource.IsCancellationRequested)
                {
                    return new List<string>();
                }

                if (SearchState == SearchState.Paused)
                {
                    _manualResetEvent.WaitOne();
                }

                var currNode = nodes.Dequeue();

                if (currNode.Attributes.Contains("href") && Helper.ValidateUri(currNode.Attributes["href"].Value))
                {
                    foundUrl.Add(currNode.Attributes["href"].Value);
                }

                var currNodeText = currNode.InnerText;

                if (currNode.InnerText.Contains(_searchedText))
                {
                    isTextFound = true;
                }

                foreach (var childNode in currNode.ChildNodes)
                {
                    nodes.Enqueue(childNode);
                }
            }

            TryUpdateScanState(isTextFound ? SearchState.Found : SearchState.NotFound);

            return foundUrl;
        }

        bool TryUpdateScanState(SearchState currentScate)
        {
            if (_visitedLinks.ContainsKey(SearchPath) &&
                _visitedLinks[SearchPath].SearchState != SearchState.Found &&
                _visitedLinks[SearchPath].SearchState != SearchState.NotFound &&
                _visitedLinks[SearchPath].SearchState != SearchState.Error)
            {
                if (_visitedLinks[SearchPath].SearchState != currentScate)
                {
                    _visitedLinks[SearchPath].SearchState = currentScate;
                    OnSearcherUpdate?.Invoke(_visitedLinks[SearchPath], null);

                    return true;
                }
            }

            return false;
        }

        public override string ToString()
        {
            return SearchState.ToString();
        }
    }
}
