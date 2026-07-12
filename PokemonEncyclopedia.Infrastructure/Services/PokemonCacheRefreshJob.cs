using IeuanWalker.Hangfire.RecurringJob.Attributes;
using Microsoft.Extensions.Logging;
using PokemonEncyclopedia.Application.Services;

namespace PokemonEncyclopedia.Infrastructure.Services;

/// <summary>
///     Hangfire recurring job that refreshes cached Pokémon catalog values.
/// </summary>
[RecurringJob("0 * * * *")]
public class PokemonCacheRefreshJob
{
    private readonly ILogger<PokemonCacheRefreshJob> _logger;
    private readonly IPokemonCatalogService _pokemonCatalogService;

    public PokemonCacheRefreshJob(
        IPokemonCatalogService pokemonCatalogService,
        ILogger<PokemonCacheRefreshJob> logger)
    {
        _pokemonCatalogService = pokemonCatalogService;
        _logger = logger;
    }

    public async Task Execute()
    {
        _logger.LogInformation("Running scheduled Pokémon cache refresh");
        await _pokemonCatalogService.RefreshAllPokemonAsync(CancellationToken.None).ConfigureAwait(false);
    }
}
