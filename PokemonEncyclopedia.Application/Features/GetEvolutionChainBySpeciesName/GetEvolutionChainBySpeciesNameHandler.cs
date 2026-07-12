using MediatR;
using Microsoft.Extensions.Logging;
using PokeApiNet;
using PokemonEncyclopedia.Application.Services;

namespace PokemonEncyclopedia.Application.Features.GetEvolutionChainBySpeciesName;

public sealed class GetEvolutionChainBySpeciesNameHandler : IRequestHandler<GetEvolutionChainBySpeciesNameQuery, EvolutionChain?>
{
    private readonly ILogger<GetEvolutionChainBySpeciesNameHandler> _logger;
    private readonly IPokemonCatalogService _pokemonCatalogService;

    public GetEvolutionChainBySpeciesNameHandler(
        IPokemonCatalogService pokemonCatalogService,
        ILogger<GetEvolutionChainBySpeciesNameHandler> logger)
    {
        _pokemonCatalogService = pokemonCatalogService;
        _logger = logger;
    }

    public async Task<EvolutionChain?> Handle(GetEvolutionChainBySpeciesNameQuery request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Handler: fetching evolution chain for {Name}", request.SpeciesName);
        return await _pokemonCatalogService.GetEvolutionChainBySpeciesNameAsync(request.SpeciesName, cancellationToken)
            .ConfigureAwait(false);
    }
}
