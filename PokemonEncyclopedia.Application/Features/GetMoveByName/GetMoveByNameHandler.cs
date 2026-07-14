using MediatR;
using Microsoft.Extensions.Logging;
using PokeApiNet;
using PokemonEncyclopedia.Application.Services;

namespace PokemonEncyclopedia.Application.Features.GetMoveByName;

public sealed class GetMoveByNameHandler : IRequestHandler<GetMoveByNameQuery, Move?>
{
    private readonly ILogger<GetMoveByNameHandler> _logger;
    private readonly IPokemonCatalogService _pokemonCatalogService;

    public GetMoveByNameHandler(
        IPokemonCatalogService pokemonCatalogService,
        ILogger<GetMoveByNameHandler> logger)
    {
        _pokemonCatalogService = pokemonCatalogService;
        _logger = logger;
    }

    public async Task<Move?> Handle(GetMoveByNameQuery request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Handler: fetching move");
        return await _pokemonCatalogService.GetMoveByNameAsync(request.Name, cancellationToken).ConfigureAwait(false);
    }
}