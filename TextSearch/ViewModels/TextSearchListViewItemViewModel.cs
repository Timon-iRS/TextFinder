using System;
using System.Windows.Input;
using System.Windows.Threading;
using TextSearch.Helpers;

namespace TextSearch.ViewModels
{
    public class TextSearchListViewItemViewModel : BaseViewModel
    {
        private readonly ITextSearcher _textSearcher;

        public string SearchingPath { get; set; }

        private string _searchState;
        public string SearchState
        {
            get => _searchState;
            set
            {
                _searchState = value;
                OnPropertyChanged(nameof(SearchState));
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

        private string _errorText = string.Empty;
        public string ErrorText
        {
            get => _errorText;
            set
            {
                _errorText = value;
                OnPropertyChanged(nameof(ErrorText));
            }
        }

        public bool CanExecute
        {
            get
            {
                return true;
            }
        }

        private ICommand _pauseResumeCommand;
        public ICommand PauseResumeCommand
        {
            get
            {
                return _pauseResumeCommand ?? (_pauseResumeCommand = new CommandHandler(() => PauseResumeAction(), () => CanExecute));
            }
        }

        public void PauseResumeAction()
        {
            _textSearcher.PauseResume();
        }

        private ICommand _stopCommand;
        public ICommand StopCommand
        {
            get
            {
                return _stopCommand ?? (_stopCommand = new CommandHandler(() => StopAction(), () => CanExecute));
            }
        }

        public void StopAction()
        {
            _textSearcher.Stop();
        }

        public TextSearchListViewItemViewModel(ITextSearcher textSearcher)
        {
            _textSearcher = textSearcher;
            textSearcher.OnSearcherUpdate += TextFinder_OnSearcherUpdate;
            SearchingPath = textSearcher.SearchPath;
            SearchState = textSearcher.SearchState.ToString();
        }

        private void TextFinder_OnSearcherUpdate(object sender, EventArgs e)
        {
            Dispatcher.Invoke(() =>
            {
                var searcher = (ITextSearcher)sender;
                SearchState = searcher.SearchState.ToString();
                ErrorText = searcher.ErrorText;
                PauseResume = searcher.SearchState == Model.SearchState.Paused ? "Resume" : "Pause";
            });
        }
    }
}
