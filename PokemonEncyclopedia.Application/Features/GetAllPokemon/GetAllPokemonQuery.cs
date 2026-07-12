using MediatR;
using PokemonEncyclopedia.Application.DTOs;

namespace PokemonEncyclopedia.Application.Features.GetAllPokemon;

public sealed record GetAllPokemonQuery : IRequest<IReadOnlyList<PokemonListItemDto>>;
