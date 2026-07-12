using PokeApiNet;

namespace PokemonEncyclopedia.Application.Services;

/// <summary>
///     Provides cached Pokémon catalog data and refresh capabilities.
/// </summary>
public interface IPokemonCatalogService
{
    bool IsWarm { get; }

    Exception? LastWarmupError { get; }

    Task<IReadOnlyList<Pokemon>> GetAllPokemonAsync(CancellationToken cancellationToken);

    Task RefreshAllPokemonAsync(CancellationToken cancellationToken);

    Task<Pokemon?> GetPokemonByNameAsync(string name, CancellationToken cancellationToken);

    Task<Move?> GetMoveByNameAsync(string name, CancellationToken cancellationToken);

    Task<PokemonSpecies?> GetPokemonSpeciesByNameAsync(string name, CancellationToken cancellationToken);

    Task<EvolutionChain?> GetEvolutionChainByIdAsync(int id, CancellationToken cancellationToken);

    Task<EvolutionChain?> GetEvolutionChainBySpeciesNameAsync(string speciesName, CancellationToken cancellationToken);

    Task<IReadOnlyList<NamedApiResource<PokemonSpecies>>> GetPokemonSpeciesByGenerationAsync(
        int generation,
        CancellationToken cancellationToken);
}
