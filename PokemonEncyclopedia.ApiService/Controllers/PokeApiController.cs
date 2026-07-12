using MediatR;
using Microsoft.AspNetCore.Mvc;
using PokeApiNet;
using PokemonEncyclopedia.Application.Models;

namespace PokemonEncyclopedia.ApiService.Controllers;

[ApiController]
[Route("api/[controller]")]
public class PokeApiController(IMediator mediator) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<Pokemon>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<Pokemon>>> GetAllPokemon(CancellationToken cancellationToken)
    {
        var pokemon = await mediator.Send(new GetAllPokemonQuery(), cancellationToken).ConfigureAwait(false);
        return Ok(pokemon);
    }

    [HttpGet("pokemon/{name}")]
    [ProducesResponseType(typeof(Pokemon), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<Pokemon>> GetPokemonByName(string name, CancellationToken cancellationToken)
    {
        var pokemon = await mediator.Send(new GetPokemonByNameQuery(name), cancellationToken).ConfigureAwait(false);
        return pokemon is null ? NotFound() : Ok(pokemon);
    }

    [HttpGet("generation/{generation:int}")]
    [ProducesResponseType(typeof(IEnumerable<NamedApiResource<PokemonSpecies>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<IEnumerable<NamedApiResource<PokemonSpecies>>>> GetPokemonByGeneration(
        int generation,
        CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new GetPokemonByGenerationQuery(generation), cancellationToken)
            .ConfigureAwait(false);
        return Ok(result);
    }
}
