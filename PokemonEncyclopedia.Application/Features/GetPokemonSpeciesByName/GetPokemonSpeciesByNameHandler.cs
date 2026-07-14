using MediatR;
using Microsoft.Extensions.Logging;
using PokeApiNet;
using PokemonEncyclopedia.Application.Services;

namespace PokemonEncyclopedia.Application.Features.GetPokemonSpeciesByName;

public sealed class GetPokemonSpeciesByNameHandler : IRequestHandler<GetPokemonSpeciesByNameQuery, PokemonSpecies?>
{
    private readonly ILogger<GetPokemonSpeciesByNameHandler> _logger;
    private readonly IPokemonCatalogService _pokemonCatalogService;

    public GetPokemonSpeciesByNameHandler(
        IPokemonCatalogService pokemonCatalogService,
        ILogger<GetPokemonSpeciesByNameHandler> logger)
    {
        _pokemonCatalogService = pokemonCatalogService;
        _logger = logger;
    }

    public async Task<PokemonSpecies?> Handle(GetPokemonSpeciesByNameQuery request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Handler: fetching species");
        return await _pokemonCatalogService.GetPokemonSpeciesByNameAsync(request.Name, cancellationToken)
            .ConfigureAwait(false);
    }
}