namespace PokemonEncyclopedia.Web;

public sealed class PokemonFilterState
{
    private string _searchText = string.Empty;
    private bool _includeLegendary = true;
    private bool _includeMythical = true;

    public PokemonFilterState()
    {
        foreach (var generation in Enumerable.Range(1, 9))
        {
            SelectedGenerations.Add(generation);
        }
    }

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

    public bool IncludeLegendary
    {
        get => _includeLegendary;
        set
        {
            if (_includeLegendary == value)
                return;

            _includeLegendary = value;
            Changed?.Invoke();
        }
    }

    public bool IncludeMythical
    {
        get => _includeMythical;
        set
        {
            if (_includeMythical == value)
                return;

            _includeMythical = value;
            Changed?.Invoke();
        }
    }

    public HashSet<int> SelectedGenerations { get; } = [];

    public HashSet<string> SelectedTypes { get; } = new(StringComparer.OrdinalIgnoreCase);

    public void ToggleGeneration(int generation)
    {
        if (!SelectedGenerations.Add(generation))
            SelectedGenerations.Remove(generation);

        Changed?.Invoke();
    }

    public void ToggleType(string type)
    {
        if (!SelectedTypes.Add(type))
            SelectedTypes.Remove(type);

        Changed?.Invoke();
    }

    public void ClearFilters()
    {
        _searchText = string.Empty;
        SelectedGenerations.Clear();
        foreach (var generation in Enumerable.Range(1, 9))
        {
            SelectedGenerations.Add(generation);
        }

        SelectedTypes.Clear();
        _includeLegendary = true;
        _includeMythical = true;
        Changed?.Invoke();
    }
}
