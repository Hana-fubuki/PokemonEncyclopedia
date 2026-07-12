using MediatR;
using PokeApiNet;
using PokemonEncyclopedia.ApiService.Models;
using PokemonEncyclopedia.ApiService.Services;

namespace PokemonEncyclopedia.ApiService.Handlers;

/// <summary>
///     Handles requests to retrieve all Pokémon species for a given generation
///     by reading from the cached Pokémon catalog.
/// </summary>
public class
    GetPokemonByGenerationHandler : IRequestHandler<GetPokemonByGenerationQuery,
    IEnumerable<NamedApiResource<PokemonSpecies>>>
{
    private readonly ILogger<GetPokemonByGenerationHandler> _logger;
    private readonly IPokemonCatalogService _pokemonCatalogService;

    /// <summary>
    ///     Initializes a new instance of the <see cref="GetPokemonByGenerationHandler" /> class.
    /// </summary>
    /// <param name="pokemonCatalogService">The shared Pokémon catalog cache service.</param>
    /// <param name="logger">Logger for diagnostic and operational messages.</param>
    public GetPokemonByGenerationHandler(
        IPokemonCatalogService pokemonCatalogService,
        ILogger<GetPokemonByGenerationHandler> logger)
    {
        _pokemonCatalogService = pokemonCatalogService;
        _logger = logger;
    }

    /// <summary>
    ///     Handles the <see cref="GetPokemonByGenerationQuery" /> by filtering cached Pokémon JSON
    ///     and returning only species that belong to the requested generation.
    /// </summary>
    /// <param name="request">The query containing the generation number to fetch.</param>
    /// <param name="cancellationToken">Cancellation token to cancel the operation.</param>
    /// <returns>
    ///     A sequence of <see cref="NamedApiResource{PokemonSpecies}" /> representing the Pokémon species
    ///     in the requested generation.
    /// </returns>
    /// <remarks>
    ///     This method reads from the shared Pokémon catalog cache.
    /// </remarks>
    public async Task<IEnumerable<NamedApiResource<PokemonSpecies>>> Handle(GetPokemonByGenerationQuery request,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("Handler: fetching generation {Generation}", request.Generation);
        var speciesInGeneration = await _pokemonCatalogService
            .GetPokemonSpeciesByGenerationAsync(request.Generation, cancellationToken)
            .ConfigureAwait(false);

        _logger.LogInformation("Handler: found {Count} species for generation {Generation}",
            speciesInGeneration.Count, request.Generation);

        return speciesInGeneration;
    }
}