using PokemonEncyclopedia.Web;
using PokemonEncyclopedia.Web.Components;

var builder = WebApplication.CreateBuilder(args);

// Add Application Insights
builder.Services.AddApplicationInsightsTelemetry();

// Add service defaults & Aspire client integrations.
builder.AddServiceDefaults();

// In integration test mode, use in-memory output cache to avoid Docker container dependencies.
var isIntegrationTestMode =
    string.Equals(builder.Configuration["INTEGRATION_TEST_MODE"], "true", StringComparison.OrdinalIgnoreCase) ||
    string.Equals(builder.Configuration["DEPLOYMENT_MODE"], "test", StringComparison.OrdinalIgnoreCase);
if (isIntegrationTestMode)
    builder.Services.AddOutputCache();
else
    builder.AddRedisOutputCache("cache");

builder.Services.AddMemoryCache();

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();
builder.Services.AddSingleton<PokemonFilterState>();
builder.Services.AddSingleton<PokemonThemeState>();

builder.Services.AddHttpClient<PokemonApiClient>(client =>
{
    // This URL uses "https+http://" to indicate HTTPS is preferred over HTTP.
    // Learn more about service discovery scheme resolution at https://aka.ms/dotnet/sdschemes.
    client.BaseAddress = new("https+http://apiservice");
});

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();

app.UseAntiforgery();

app.UseOutputCache();

app.MapStaticAssets();

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.MapDefaultEndpoints();

app.Run();