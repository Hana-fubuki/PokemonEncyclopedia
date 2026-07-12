using MediatR;
using PokeApiNet;

namespace PokemonEncyclopedia.Application.Features.GetEvolutionChainBySpeciesName;

public record GetEvolutionChainBySpeciesNameQuery(string SpeciesName) : IRequest<EvolutionChain?>;
