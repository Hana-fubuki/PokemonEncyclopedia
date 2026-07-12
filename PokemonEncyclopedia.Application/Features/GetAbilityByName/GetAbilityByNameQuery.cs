using MediatR;
using PokeApiNet;

namespace PokemonEncyclopedia.Application.Features.GetAbilityByName;

public sealed record GetAbilityByNameQuery(string Name) : IRequest<Ability?>;
