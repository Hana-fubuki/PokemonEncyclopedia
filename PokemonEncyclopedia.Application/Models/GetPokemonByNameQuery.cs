using MediatR;
using PokeApiNet;

namespace PokemonEncyclopedia.Application.Models;

public record GetPokemonByNameQuery(string Name) : IRequest<Pokemon?>;
