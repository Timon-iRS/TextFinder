using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using TextSearch.Helpers;
using TextSearch.Models;

namespace TextSearch
{
    public class WebsiteTextSearchController
    {
        private ManualResetEvent _manualResetEvent;
        private CancellationTokenSource _cancelTokenSource;
        private CancellationToken _token;

        private string _startUrl;
        private int _maxThreadCount;
        private string _searchText;
        private int _scanUrlLimit;

        public ControllerState SearchControllerState { get; set; }
        public SizeLimitedDictionary<string, ITextSearcher> VisitedLinks { get; private set; }

        public event EventHandler OnSearcherAdded;

        public void Init(string startUrl, int maxTheadCount, string searchText, int scanUrlLimit)
        {
            _startUrl = startUrl;
            _maxThreadCount = maxTheadCount > 10 ? 10 : maxTheadCount;
            _searchText = searchText;
            _scanUrlLimit = scanUrlLimit;

            VisitedLinks = new SizeLimitedDictionary<string, ITextSearcher>(_scanUrlLimit);

            _manualResetEvent = new ManualResetEvent(false);
            _cancelTokenSource = new CancellationTokenSource();
            _token = _cancelTokenSource.Token;
        }

        public async Task StartAsync()
        {
            SearchControllerState = ControllerState.Search;
            var websiteProcess = new WebsiteTextSearcher(_startUrl, VisitedLinks, _searchText);
            VisitedLinks.TryAdd(_startUrl, websiteProcess);

            OnSearcherAdded?.Invoke(websiteProcess, null);

            var foundUrls = await websiteProcess.StartAsync();
            await ProcessFoundUrls(foundUrls);
            SearchControllerState = ControllerState.End;
        }

        public void PauseResumeAll()
        {
            switch (SearchControllerState)
            {
                case ControllerState.Pause:

                    _manualResetEvent.Set();
                    _manualResetEvent = new ManualResetEvent(false);

                    SearchControllerState = ControllerState.Search;

                    for (int i = 0; i < VisitedLinks.Count; i++)
                    {
                        VisitedLinks.ElementAt(i).Value.PauseResume();
                    }
                    break;
                case ControllerState.Search:
                    SearchControllerState = ControllerState.Pause;
                    for (int i = 0; i < VisitedLinks.Count; i++)
                    {
                        VisitedLinks.ElementAt(i).Value.PauseResume();
                    }
                    break;
            }
        }

        public void StopAll()
        {
            _cancelTokenSource.Cancel();
            SearchControllerState = ControllerState.Cancel;
        }

        public async Task ProcessFoundUrls(List<string> foundUrls)
        {
            var tasks = new List<Task<List<string>>>();

            using (SemaphoreSlim concurrencySemaphore = new SemaphoreSlim(_maxThreadCount))
            {
                foreach (var foundUrl in foundUrls)
                {
                    concurrencySemaphore.Wait(_token);

                    if (SearchControllerState == ControllerState.Pause)
                    {
                        _manualResetEvent.WaitOne();
                    }

                    var websiteTask = Task.Run(async () =>
                    {
                        try
                        {
                            if (Helper.ValidateUri(foundUrl))
                            {
                                if (VisitedLinks.TryAdd(foundUrl, null))
                                {
                                    var process = new WebsiteTextSearcher(foundUrl, VisitedLinks, _searchText);
                                    VisitedLinks[foundUrl] = process;

                                    OnSearcherAdded?.Invoke(process, null);

                                    return await process.StartAsync();
                                }
                                return new List<string>();
                            }
                            return new List<string>();
                        }
                        finally
                        {
                            concurrencySemaphore.Release();
                        }
                    }, _token);

                    tasks.Add(websiteTask);
                }

                await Task.WhenAll(tasks.ToArray());
            }

            foreach (var task in tasks)
            {
                if (task.Result?.Count > 0)
                    await ProcessFoundUrls(task.Result);
            }
        }
    }
}
