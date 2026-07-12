using MediatR;
using PokeApiNet;

namespace PokemonEncyclopedia.Application.Features.GetMoveByName;

public record GetMoveByNameQuery(string Name) : IRequest<Move?>;
