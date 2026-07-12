using MediatR;
using PokeApiNet;

namespace PokemonEncyclopedia.Application.Models;

public sealed record GetAllPokemonQuery : IRequest<IReadOnlyList<Pokemon>>;
