namespace PokemonEncyclopedia.Web;

public sealed class PokemonThemeState
{
    public event Action? Changed;

    public bool IsDarkMode { get; private set; } = true;

    public void Toggle()
    {
        IsDarkMode = !IsDarkMode;
        Changed?.Invoke();
    }
}
