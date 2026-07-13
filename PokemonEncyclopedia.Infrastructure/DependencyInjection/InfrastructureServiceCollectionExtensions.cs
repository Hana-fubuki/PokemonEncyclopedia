using Microsoft.Extensions.DependencyInjection;
using PokeApiNet;
using PokemonEncyclopedia.Application.Services;
using PokemonEncyclopedia.Infrastructure.Services;

namespace PokemonEncyclopedia.Infrastructure.DependencyInjection;

public static class InfrastructureServiceCollectionExtensions
{
    public static IServiceCollection AddInfrastructureServices(this IServiceCollection services)
    {
        services.AddSingleton<PokeApiClient>();

        // Register HttpClient with longer timeout for pokemon-species pagination
        services.AddHttpClient();

        services.AddSingleton<IPokemonCatalogService>(sp =>
        {
            var httpClientFactory = sp.GetRequiredService<IHttpClientFactory>();
            var httpClient = httpClientFactory.CreateClient();
            httpClient.BaseAddress = new Uri("https://pokeapi.co/api/v2/");
            httpClient.Timeout = TimeSpan.FromSeconds(300);
            
            var cache = sp.GetRequiredService<Microsoft.Extensions.Caching.Distributed.IDistributedCache>();
            var logger = sp.GetRequiredService<Microsoft.Extensions.Logging.ILogger<PokemonCatalogService>>();
            var pokeApiClient = sp.GetRequiredService<PokeApiClient>();

            return new PokemonCatalogService(pokeApiClient, httpClient, cache, logger);
        });

        services.AddTransient<PokemonCacheRefreshJob>();

        return services;
    }
}
