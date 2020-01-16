using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using TextSearch.Model;

namespace TextSearch
{
    public interface ITextSearcher
    {
        string SearchPath { get; }
        SearchState SearchState { get; set; }
        string ErrorText { get; }
        event EventHandler OnSearcherUpdate;

        Task<List<string>> StartAsync();
        void PauseResume();
        void Stop();

    }
}
