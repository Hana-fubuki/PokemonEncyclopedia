using MediatR;
using PokeApiNet;

namespace PokemonEncyclopedia.Application.Features.GetPokemonByName;

public record GetPokemonByNameQuery(string Name) : IRequest<Pokemon?>;
