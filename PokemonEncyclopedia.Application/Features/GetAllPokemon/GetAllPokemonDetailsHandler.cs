using MediatR;
using Microsoft.Extensions.Logging;
using PokeApiNet;
using PokemonEncyclopedia.Application.Services;

namespace PokemonEncyclopedia.Application.Features.GetAllPokemon;

public sealed class GetAllPokemonDetailsHandler : IRequestHandler<GetAllPokemonDetailsQuery, IReadOnlyList<Pokemon>>
{
    private readonly ILogger<GetAllPokemonDetailsHandler> _logger;
    private readonly IPokemonCatalogService _pokemonCatalogService;

    public GetAllPokemonDetailsHandler(
        IPokemonCatalogService pokemonCatalogService,
        ILogger<GetAllPokemonDetailsHandler> logger)
    {
        _pokemonCatalogService = pokemonCatalogService;
        _logger = logger;
    }

    public async Task<IReadOnlyList<Pokemon>> Handle(GetAllPokemonDetailsQuery request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Handler: fetching all pokemon details with sprites");
        return await _pokemonCatalogService.GetAllPokemonAsync(cancellationToken).ConfigureAwait(false);
    }
}
