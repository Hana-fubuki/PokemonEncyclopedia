using MediatR;
using PokeApiNet;

namespace PokemonEncyclopedia.Application.Features.GetAllPokemon;

public sealed record GetAllPokemonDetailsQuery : IRequest<IReadOnlyList<Pokemon>>;
