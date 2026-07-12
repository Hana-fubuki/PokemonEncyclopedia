using Microsoft.JSInterop;

namespace PokemonEncyclopedia.Web;

public sealed class PokemonThemeState
{
    private IJSRuntime? _jsRuntime;
    public event Action? Changed;

    public bool IsDarkMode { get; private set; } = true;

    public async Task InitializeAsync(IJSRuntime jsRuntime)
    {
        _jsRuntime = jsRuntime;
        try
        {
            var savedTheme = await jsRuntime.InvokeAsync<string>("themeInterop.getTheme");
            IsDarkMode = savedTheme == "dark";
        }
        catch
        {
            // JS interop not available, use default
            IsDarkMode = true;
        }
    }

    public async Task Toggle()
    {
        IsDarkMode = !IsDarkMode;
        if (_jsRuntime != null)
        {
            try
            {
                var themeName = IsDarkMode ? "dark" : "light";
                await _jsRuntime.InvokeVoidAsync("themeInterop.setTheme", themeName);
            }
            catch
            {
                // JS interop not available
            }
        }
        Changed?.Invoke();
    }
}
