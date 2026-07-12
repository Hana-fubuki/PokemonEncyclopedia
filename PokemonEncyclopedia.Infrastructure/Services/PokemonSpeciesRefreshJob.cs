using IeuanWalker.Hangfire.RecurringJob.Attributes;
using Microsoft.Extensions.Logging;
using PokemonEncyclopedia.Application.Services;

namespace PokemonEncyclopedia.Infrastructure.Services;

/// <summary>
///     Hangfire recurring job that refreshes cached Pokémon species data daily.
/// </summary>
[RecurringJob("0 3 * * *")]
public class PokemonSpeciesRefreshJob
{
    private readonly ILogger<PokemonSpeciesRefreshJob> _logger;
    private readonly IPokemonCatalogService _pokemonCatalogService;

    public PokemonSpeciesRefreshJob(
        IPokemonCatalogService pokemonCatalogService,
        ILogger<PokemonSpeciesRefreshJob> logger)
    {
        _pokemonCatalogService = pokemonCatalogService;
        _logger = logger;
    }

    public async Task Execute()
    {
        _logger.LogInformation("Running scheduled Pokémon species cache refresh");
        await _pokemonCatalogService.GetAllPokemonSpeciesAsync(CancellationToken.None).ConfigureAwait(false);
        _logger.LogInformation("Pokémon species cache refresh completed");
    }
}
