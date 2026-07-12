using MediatR;
using Microsoft.AspNetCore.Mvc;
using PokeApiNet;
using PokemonEncyclopedia.Application.Features.GetAllPokemon;
using PokemonEncyclopedia.Application.Features.GetAllAbilities;
using PokemonEncyclopedia.Application.Features.GetAbilityByName;
using PokemonEncyclopedia.Application.Features.GetEvolutionChainBySpeciesName;
using PokemonEncyclopedia.Application.Features.GetMoveByName;
using PokemonEncyclopedia.Application.Features.GetPokemonByGeneration;
using PokemonEncyclopedia.Application.Features.GetPokemonByName;
using PokemonEncyclopedia.Application.Features.GetPokemonSpeciesByName;
using PokemonEncyclopedia.Application.Services;

namespace PokemonEncyclopedia.ApiService.Controllers;

[ApiController]
[Route("api/[controller]")]
public class PokeApiController(IMediator mediator) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<PokemonSpecies>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<Pokemon>>> GetAllPokemon(CancellationToken cancellationToken)
    {
        var pokemon = await mediator.Send(new GetAllPokemonQuery(), cancellationToken).ConfigureAwait(false);
        return Ok(pokemon);
    }

    [HttpGet("details")]
    [ProducesResponseType(typeof(IReadOnlyList<Pokemon>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<Pokemon>>> GetAllPokemonDetails(CancellationToken cancellationToken)
    {
        var pokemon = await mediator.Send(new GetAllPokemonDetailsQuery(), cancellationToken).ConfigureAwait(false);
        return Ok(pokemon);
    }

    [HttpGet("pokemon/{name}")]
    [ProducesResponseType(typeof(Pokemon), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
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

    [HttpGet("move/{name}")]
    [ProducesResponseType(typeof(Move), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<Move>> GetMoveByName(string name, CancellationToken cancellationToken)
    {
        var move = await mediator.Send(new GetMoveByNameQuery(name), cancellationToken).ConfigureAwait(false);
        if (move is null)
            return NotFound();

        return Ok(move);
    }

    [HttpGet("species/{name}")]
    [ProducesResponseType(typeof(PokemonSpecies), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<PokemonSpecies>> GetSpeciesByName(string name, CancellationToken cancellationToken)
    {
        var species = await mediator.Send(new GetPokemonSpeciesByNameQuery(name), cancellationToken).ConfigureAwait(false);
        if (species is null)
            return NotFound();

        return Ok(species);
    }

    [HttpGet("evolution-chain/{id:int}")]
    [ProducesResponseType(typeof(EvolutionChain), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<EvolutionChain>> GetEvolutionChain(int id, [FromServices] IPokemonCatalogService catalogService, CancellationToken cancellationToken)
    {
        var evolutionChain = await catalogService.GetEvolutionChainByIdAsync(id, cancellationToken).ConfigureAwait(false);
        if (evolutionChain is null)
            return NotFound();

        return Ok(evolutionChain);
    }

    [HttpGet("evolution/{speciesName}")]
    [ProducesResponseType(typeof(EvolutionChain), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<EvolutionChain>> GetEvolutionChainBySpecies(
        string speciesName,
        CancellationToken cancellationToken)
    {
        var evolutionChain = await mediator.Send(new GetEvolutionChainBySpeciesNameQuery(speciesName), cancellationToken)
            .ConfigureAwait(false);
        if (evolutionChain is null)
            return NotFound();

        return Ok(evolutionChain);
    }

    [HttpGet("legendary")]
    [ProducesResponseType(typeof(IReadOnlySet<string>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlySet<string>>> GetLegendaryPokemonNames(
        [FromServices] IPokemonCatalogService catalogService,
        CancellationToken cancellationToken)
    {
        var legendaryNames = await catalogService.GetLegendaryPokemonNamesAsync(cancellationToken).ConfigureAwait(false);
        return Ok(legendaryNames);
    }

    [HttpGet("abilities")]
    [ProducesResponseType(typeof(IReadOnlyList<Ability>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<Ability>>> GetAllAbilities(CancellationToken cancellationToken)
    {
        var abilities = await mediator.Send(new GetAllAbilitiesQuery(), cancellationToken).ConfigureAwait(false);
        return Ok(abilities);
    }

    [HttpGet("ability/{name}")]
    [ProducesResponseType(typeof(Ability), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<Ability>> GetAbilityByName(string name, CancellationToken cancellationToken)
    {
        var ability = await mediator.Send(new GetAbilityByNameQuery(name), cancellationToken).ConfigureAwait(false);
        if (ability is null)
            return NotFound();

        return Ok(ability);
    }
}
