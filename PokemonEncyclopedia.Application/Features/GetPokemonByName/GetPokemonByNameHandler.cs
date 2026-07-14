using MediatR;
using Microsoft.Extensions.Logging;
using PokeApiNet;
using PokemonEncyclopedia.Application.Services;

namespace PokemonEncyclopedia.Application.Features.GetPokemonByName;

public sealed class GetPokemonByNameHandler : IRequestHandler<GetPokemonByNameQuery, Pokemon?>
{
    private readonly ILogger<GetPokemonByNameHandler> _logger;
    private readonly IPokemonCatalogService _pokemonCatalogService;

    public GetPokemonByNameHandler(
        IPokemonCatalogService pokemonCatalogService,
        ILogger<GetPokemonByNameHandler> logger)
    {
        _pokemonCatalogService = pokemonCatalogService;
        _logger = logger;
    }

    public async Task<Pokemon?> Handle(GetPokemonByNameQuery request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Handler: fetching pokemon");
        var pokemon = await _pokemonCatalogService.GetPokemonByNameAsync(request.Name, cancellationToken)
            .ConfigureAwait(false);
        if (pokemon is null)
        {
            _logger.LogWarning("Pokemon was not found in cache");
            return null;
        }

        return pokemon;
    }
}