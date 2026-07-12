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
        services.AddSingleton<IPokemonCatalogService, PokemonCatalogService>();
        services.AddTransient<PokemonCacheRefreshJob>();

        return services;
    }
}
