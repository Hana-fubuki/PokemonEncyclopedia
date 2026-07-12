namespace PokemonEncyclopedia.Web;

public sealed class PokemonSearchState
{
    private string _searchText = string.Empty;

    public event Action? Changed;

    public string SearchText
    {
        get => _searchText;
        set
        {
            var normalized = value ?? string.Empty;
            if (string.Equals(_searchText, normalized, StringComparison.Ordinal))
                return;

            _searchText = normalized;
            Changed?.Invoke();
        }
    }
}
