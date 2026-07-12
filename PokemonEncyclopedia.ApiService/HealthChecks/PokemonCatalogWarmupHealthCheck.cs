using Microsoft.Extensions.Diagnostics.HealthChecks;
using PokemonEncyclopedia.Application.Services;

namespace PokemonEncyclopedia.ApiService.HealthChecks;

/// <summary>
///     Health check that reports readiness based on Pokémon catalog warmup state.
/// </summary>
public class PokemonCatalogWarmupHealthCheck : IHealthCheck
{
    private readonly IPokemonCatalogService _pokemonCatalogService;

    public PokemonCatalogWarmupHealthCheck(IPokemonCatalogService pokemonCatalogService)
    {
        _pokemonCatalogService = pokemonCatalogService;
    }

    public Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        if (_pokemonCatalogService.IsWarm)
            return Task.FromResult(HealthCheckResult.Healthy("Pokémon catalog is warmed"));

        if (_pokemonCatalogService.LastWarmupError is not null)
            return Task.FromResult(HealthCheckResult.Unhealthy(
                "Pokémon catalog warmup failed",
                _pokemonCatalogService.LastWarmupError));

        return Task.FromResult(HealthCheckResult.Degraded("Pokémon catalog warmup in progress"));
    }
}
