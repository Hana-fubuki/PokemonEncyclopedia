using MediatR;
using PokeApiNet;

namespace PokemonEncyclopedia.ApiService.Models;

/// <summary>
///     Integer that represents the pokemon generation to get the pokemon species from
/// </summary>
/// <param name="Generation">Generation number used to query species (1-9).</param>
public record GetPokemonByGenerationQuery(int Generation) : IRequest<IEnumerable<NamedApiResource<PokemonSpecies>>>;