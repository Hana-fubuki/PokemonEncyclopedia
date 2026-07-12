using MediatR;
using PokeApiNet;

namespace PokemonEncyclopedia.Application.Features.GetAllPokemon;

public sealed record GetAllPokemonQuery : IRequest<IReadOnlyList<Pokemon>>;
