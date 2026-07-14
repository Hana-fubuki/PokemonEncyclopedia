using MediatR;
using Microsoft.Extensions.Logging;
using PokeApiNet;
using PokemonEncyclopedia.Application.Services;

namespace PokemonEncyclopedia.Application.Features.GetAbilityByName;

public sealed class GetAbilityByNameHandler : IRequestHandler<GetAbilityByNameQuery, Ability?>
{
    private readonly ILogger<GetAbilityByNameHandler> _logger;
    private readonly IPokemonCatalogService _pokemonCatalogService;

    public GetAbilityByNameHandler(
        IPokemonCatalogService pokemonCatalogService,
        ILogger<GetAbilityByNameHandler> logger)
    {
        _pokemonCatalogService = pokemonCatalogService;
        _logger = logger;
    }

    public async Task<Ability?> Handle(GetAbilityByNameQuery request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Handler: fetching ability");
        return await _pokemonCatalogService.GetAbilityByNameAsync(request.Name, cancellationToken)
            .ConfigureAwait(false);
    }
}