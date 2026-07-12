using IeuanWalker.Hangfire.RecurringJob.Attributes;
using Microsoft.Extensions.Logging;
using PokemonEncyclopedia.Application.Services;

namespace PokemonEncyclopedia.Infrastructure.Services;

/// <summary>
///     Hangfire recurring job that refreshes cached Pokémon moves data daily.
/// </summary>
[RecurringJob("0 2 * * *")]
public class PokemonMovesRefreshJob
{
    private readonly ILogger<PokemonMovesRefreshJob> _logger;
    private readonly IPokemonCatalogService _pokemonCatalogService;

    public PokemonMovesRefreshJob(
        IPokemonCatalogService pokemonCatalogService,
        ILogger<PokemonMovesRefreshJob> logger)
    {
        _pokemonCatalogService = pokemonCatalogService;
        _logger = logger;
    }

    public async Task Execute()
    {
        _logger.LogInformation("Running scheduled Pokémon moves cache refresh");
        _ = await _pokemonCatalogService.GetAllMovesAsync(CancellationToken.None).ConfigureAwait(false);
        _logger.LogInformation("Pokémon moves cache refresh completed");
    }
}
