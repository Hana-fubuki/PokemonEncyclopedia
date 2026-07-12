using MediatR;
using PokeApiNet;

namespace PokemonEncyclopedia.Application.Features.GetPokemonSpeciesByName;

public record GetPokemonSpeciesByNameQuery(string Name) : IRequest<PokemonSpecies?>;
