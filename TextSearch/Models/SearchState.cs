namespace TextSearch.Model
{
    public enum SearchState
    {
        Unknown,
        Download,
        Scan,
        Found,
        NotFound,
        Paused,
        Canceled,
        Error
    }
}
