using MediatR;
using Microsoft.Extensions.Logging;
using PokemonEncyclopedia.Application.DTOs;
using PokemonEncyclopedia.Application.Services;

namespace PokemonEncyclopedia.Application.Features.GetAllPokemon;

public sealed class GetAllPokemonHandler : IRequestHandler<GetAllPokemonQuery, IReadOnlyList<PokemonListItemDto>>
{
    private readonly ILogger<GetAllPokemonHandler> _logger;
    private readonly IPokemonCatalogService _pokemonCatalogService;

    public GetAllPokemonHandler(
        IPokemonCatalogService pokemonCatalogService,
        ILogger<GetAllPokemonHandler> logger)
    {
        _pokemonCatalogService = pokemonCatalogService;
        _logger = logger;
    }

    public async Task<IReadOnlyList<PokemonListItemDto>> Handle(GetAllPokemonQuery request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Handler: fetching all pokemon");
        var pokemon = await _pokemonCatalogService.GetAllPokemonAsync(cancellationToken).ConfigureAwait(false);
        var legendary = await _pokemonCatalogService.GetLegendaryPokemonNamesAsync(cancellationToken).ConfigureAwait(false);

        // Return lightweight list with just essential fields for display
        return pokemon
            .Select(p => new PokemonListItemDto
            {
                Id = p.Id,
                Name = p.Name,
                ImageUrl = p.Sprites?.FrontDefault ?? string.Empty,
                IsLegendary = legendary.Contains(p.Species.Name),
                IsMythical = false // Will be populated from species data on detail page
            })
            .ToList()
            .AsReadOnly();
    }
}
