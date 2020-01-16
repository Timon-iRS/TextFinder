using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System.Windows.Threading;
using TextSearch.Helpers;
using TextSearch.Models;

namespace TextSearch.ViewModels
{
    public class MainWindowViewModel : BaseViewModel
    {
        private WebsiteTextSearchController _searchController;

        public ObservableCollection<TextSearchListViewItemViewModel> TextFinders { get; set; } = new ObservableCollection<TextSearchListViewItemViewModel>();

        private string _searchStatus;
        public string SearchStatus
        {
            get => _searchStatus;
            set
            {
                _searchStatus = value;
                OnPropertyChanged(nameof(SearchStatus));
            }
        }

        private string _pauseResume = "Pause";
        public string PauseResume
        {
            get => _pauseResume;
            set
            {
                _pauseResume = value;
                OnPropertyChanged(nameof(PauseResume));
            }
        }

        public MainWindowViewModel()
        {
            _searchController = new WebsiteTextSearchController();
            _searchController.OnSearcherAdded += SearchController_OnSearcherAdded;
        }

        private void SearchController_OnSearcherAdded(object sender, EventArgs e)
        {
            Dispatcher.Invoke(() => TextFinders.Add(new TextSearchListViewItemViewModel((ITextSearcher)sender)));
        }

        public void StartSearch(string startUrl, int maxThreadCount, string searchText, int maxUrlCount)
        {
            if (Helper.ValidateUri(startUrl))
            {
                TextFinders.Clear();
                _searchController.Init(startUrl, maxThreadCount, searchText, maxUrlCount);
                Task.Run(async () =>
                {
                    await _searchController.StartAsync().ContinueWith((t) =>
                    {
                        Dispatcher.Invoke(() => SearchStatus = _searchController.SearchControllerState.ToString());
                    });
                });
                SearchStatus = _searchController.SearchControllerState.ToString();
            }
            else
            {
                SearchStatus = "Invalid Url!";
            }
        }

        public void PauseResumeSearch()
        {
            _searchController.PauseResumeAll();
            PauseResume = _searchController.SearchControllerState == ControllerState.Pause ? "Resume" : "Pause";
            SearchStatus = _searchController.SearchControllerState.ToString();
        }

        public void StopSearch()
        {
            _searchController.StopAll();
        }
    }
}

