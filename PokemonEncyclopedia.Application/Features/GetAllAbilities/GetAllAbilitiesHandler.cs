using MediatR;
using Microsoft.Extensions.Logging;
using PokeApiNet;
using PokemonEncyclopedia.Application.Services;

namespace PokemonEncyclopedia.Application.Features.GetAllAbilities;

public sealed class GetAllAbilitiesHandler : IRequestHandler<GetAllAbilitiesQuery, IReadOnlyList<Ability>>
{
    private readonly ILogger<GetAllAbilitiesHandler> _logger;
    private readonly IPokemonCatalogService _pokemonCatalogService;

    public GetAllAbilitiesHandler(
        IPokemonCatalogService pokemonCatalogService,
        ILogger<GetAllAbilitiesHandler> logger)
    {
        _pokemonCatalogService = pokemonCatalogService;
        _logger = logger;
    }

    public async Task<IReadOnlyList<Ability>> Handle(GetAllAbilitiesQuery request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Handler: fetching all abilities");
        return await _pokemonCatalogService.GetAllAbilitiesAsync(cancellationToken).ConfigureAwait(false);
    }
}
