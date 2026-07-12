using MediatR;
using PokeApiNet;

namespace PokemonEncyclopedia.Application.Features.GetAllAbilities;

public sealed record GetAllAbilitiesQuery : IRequest<IReadOnlyList<Ability>>;
