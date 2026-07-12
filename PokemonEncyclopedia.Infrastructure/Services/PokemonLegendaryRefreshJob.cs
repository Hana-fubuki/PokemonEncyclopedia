using IeuanWalker.Hangfire.RecurringJob.Attributes;
using Microsoft.Extensions.Logging;
using PokemonEncyclopedia.Application.Services;

namespace PokemonEncyclopedia.Infrastructure.Services;

/// <summary>
///     Hangfire recurring job that refreshes cached Pokémon legendary data daily.
/// </summary>
[RecurringJob("0 4 * * *")]
public class PokemonLegendaryRefreshJob
{
    private readonly ILogger<PokemonLegendaryRefreshJob> _logger;
    private readonly IPokemonCatalogService _pokemonCatalogService;

    public PokemonLegendaryRefreshJob(
        IPokemonCatalogService pokemonCatalogService,
        ILogger<PokemonLegendaryRefreshJob> logger)
    {
        _pokemonCatalogService = pokemonCatalogService;
        _logger = logger;
    }

    public async Task Execute()
    {
        _logger.LogInformation("Running scheduled Pokémon legendary data cache refresh");
        _ = await _pokemonCatalogService.GetLegendaryPokemonNamesAsync(CancellationToken.None).ConfigureAwait(false);
        _logger.LogInformation("Pokémon legendary data cache refresh completed");
    }
}
