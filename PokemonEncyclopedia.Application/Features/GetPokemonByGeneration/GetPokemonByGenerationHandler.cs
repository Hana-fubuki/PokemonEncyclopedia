using MediatR;
using Microsoft.Extensions.Logging;
using PokeApiNet;
using PokemonEncyclopedia.Application.Services;

namespace PokemonEncyclopedia.Application.Features.GetPokemonByGeneration;

public sealed class GetPokemonByGenerationHandler
    : IRequestHandler<GetPokemonByGenerationQuery, IEnumerable<NamedApiResource<PokemonSpecies>>>
{
    private readonly ILogger<GetPokemonByGenerationHandler> _logger;
    private readonly IPokemonCatalogService _pokemonCatalogService;

    public GetPokemonByGenerationHandler(
        IPokemonCatalogService pokemonCatalogService,
        ILogger<GetPokemonByGenerationHandler> logger)
    {
        _pokemonCatalogService = pokemonCatalogService;
        _logger = logger;
    }

    public async Task<IEnumerable<NamedApiResource<PokemonSpecies>>> Handle(GetPokemonByGenerationQuery request,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("Handler: fetching generation");
        var speciesInGeneration = await _pokemonCatalogService
            .GetPokemonSpeciesByGenerationAsync(request.Generation, cancellationToken)
            .ConfigureAwait(false);

        _logger.LogInformation("Handler: found species for generation");

        return speciesInGeneration;
    }
}