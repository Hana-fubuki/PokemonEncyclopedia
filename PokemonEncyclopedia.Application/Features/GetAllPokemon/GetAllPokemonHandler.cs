using MediatR;
using Microsoft.Extensions.Logging;
using PokeApiNet;
using PokemonEncyclopedia.Application.Services;

namespace PokemonEncyclopedia.Application.Features.GetAllPokemon;

public sealed class GetAllPokemonHandler : IRequestHandler<GetAllPokemonQuery, IReadOnlyList<Pokemon>>
{
    private readonly ILogger<GetAllPokemonHandler> _logger;
    private readonly IPokemonCatalogService _pokemonCatalogService;

    public GetAllPokemonHandler(
        IPokemonCatalogService pokemonCatalogService,
        ILogger<GetAllPokemonHandler> logger)
    {
        _pokemonCatalogService = pokemonCatalogService;
        _logger = logger;
    }

    public async Task<IReadOnlyList<Pokemon>> Handle(GetAllPokemonQuery request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Handler: fetching all pokemon");
        return await _pokemonCatalogService.GetAllPokemonAsync(cancellationToken).ConfigureAwait(false);
    }
}
