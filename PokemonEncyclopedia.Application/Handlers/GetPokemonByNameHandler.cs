using MediatR;
using Microsoft.Extensions.Logging;
using PokeApiNet;
using PokemonEncyclopedia.Application.Models;
using PokemonEncyclopedia.Application.Services;

namespace PokemonEncyclopedia.Application.Handlers;

public class GetPokemonByNameHandler : IRequestHandler<GetPokemonByNameQuery, Pokemon?>
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
        _logger.LogInformation("Handler: fetching pokemon {Name}", request.Name);
        var pokemon = await _pokemonCatalogService.GetPokemonByNameAsync(request.Name, cancellationToken).ConfigureAwait(false);
        if (pokemon is null)
        {
            _logger.LogWarning("Pokemon {Name} was not found in cache", request.Name);
            return null;
        }

        return pokemon;
    }
}
