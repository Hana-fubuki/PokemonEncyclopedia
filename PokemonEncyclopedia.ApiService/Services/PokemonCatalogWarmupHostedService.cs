using PokemonEncyclopedia.Application.Services;

namespace PokemonEncyclopedia.ApiService.Services;

/// <summary>
///     Background service that warms the Pokémon catalog cache during startup.
/// </summary>
public class PokemonCatalogWarmupHostedService : BackgroundService
{
    private readonly ILogger<PokemonCatalogWarmupHostedService> _logger;
    private readonly IPokemonCatalogService _pokemonCatalogService;

    public PokemonCatalogWarmupHostedService(
        IPokemonCatalogService pokemonCatalogService,
        ILogger<PokemonCatalogWarmupHostedService> logger)
    {
        _pokemonCatalogService = pokemonCatalogService;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            await _pokemonCatalogService.GetAllPokemonAsync(stoppingToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Pokémon catalog warmup failed");
        }
    }
}
