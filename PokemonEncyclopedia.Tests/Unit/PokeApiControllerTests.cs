using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using PokemonEncyclopedia.ApiService.Controllers;
using PokemonEncyclopedia.Application.Features.GetAbilityByName;
using PokemonEncyclopedia.Application.Features.GetAllAbilities;
using PokemonEncyclopedia.Application.Features.GetAllPokemon;
using PokemonEncyclopedia.Application.Features.GetEvolutionChainBySpeciesName;
using PokemonEncyclopedia.Application.Features.GetMoveByName;
using PokemonEncyclopedia.Application.Features.GetPokemonByGeneration;
using PokemonEncyclopedia.Application.Features.GetPokemonByName;
using PokemonEncyclopedia.Application.Features.GetPokemonSpeciesByName;
using PokemonEncyclopedia.Application.Services;
using PokeApiNet;

namespace PokemonEncyclopedia.Tests.Unit;

public class PokeApiControllerTests
{
    [Fact]
    public async Task GetAllPokemon_ReturnsOk()
    {
        var mediator = new Mock<IMediator>();
        var expected = new List<PokemonSpecies> { new() { Id = 1, Name = "bulbasaur" } };
        mediator.Setup(m => m.Send(It.IsAny<GetAllPokemonQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expected);

        var controller = new PokeApiController(mediator.Object);

        var result = await controller.GetAllPokemon(CancellationToken.None);

        result.Result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task GetAllPokemonDetails_ReturnsOk()
    {
        var mediator = new Mock<IMediator>();
        var expected = new List<Pokemon> { new() { Id = 25, Name = "pikachu" } };
        mediator.Setup(m => m.Send(It.IsAny<GetAllPokemonDetailsQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expected);

        var controller = new PokeApiController(mediator.Object);

        var result = await controller.GetAllPokemonDetails(CancellationToken.None);

        result.Result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task GetPokemonByName_ReturnsNotFoundWhenMissing()
    {
        var mediator = new Mock<IMediator>();
        mediator.Setup(m => m.Send(It.IsAny<GetPokemonByNameQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Pokemon?)null);

        var controller = new PokeApiController(mediator.Object);

        var result = await controller.GetPokemonByName("missingno", CancellationToken.None);

        result.Result.Should().BeOfType<NotFoundResult>();
    }

    [Fact]
    public async Task GetPokemonByName_ReturnsOkWhenFound()
    {
        var mediator = new Mock<IMediator>();
        mediator.Setup(m => m.Send(It.IsAny<GetPokemonByNameQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Pokemon { Id = 25, Name = "pikachu" });

        var controller = new PokeApiController(mediator.Object);

        var result = await controller.GetPokemonByName("pikachu", CancellationToken.None);

        result.Result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task GetPokemonByGeneration_ReturnsOk()
    {
        var mediator = new Mock<IMediator>();
        mediator.Setup(m => m.Send(It.IsAny<GetPokemonByGenerationQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<NamedApiResource<PokemonSpecies>>
            {
                new() { Name = "bulbasaur", Url = "https://pokeapi.co/api/v2/pokemon-species/1/" }
            });

        var controller = new PokeApiController(mediator.Object);

        var result = await controller.GetPokemonByGeneration(1, CancellationToken.None);

        result.Result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task GetMoveByName_ReturnsNotFoundWhenMissing()
    {
        var mediator = new Mock<IMediator>();
        mediator.Setup(m => m.Send(It.IsAny<GetMoveByNameQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Move?)null);

        var controller = new PokeApiController(mediator.Object);

        var result = await controller.GetMoveByName("missing", CancellationToken.None);

        result.Result.Should().BeOfType<NotFoundResult>();
    }

    [Fact]
    public async Task GetSpeciesByName_ReturnsNotFoundWhenMissing()
    {
        var mediator = new Mock<IMediator>();
        mediator.Setup(m => m.Send(It.IsAny<GetPokemonSpeciesByNameQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((PokemonSpecies?)null);

        var controller = new PokeApiController(mediator.Object);

        var result = await controller.GetSpeciesByName("missing", CancellationToken.None);

        result.Result.Should().BeOfType<NotFoundResult>();
    }

    [Fact]
    public async Task GetEvolutionChain_ReturnsOk()
    {
        var catalog = new Mock<IPokemonCatalogService>();
        catalog.Setup(s => s.GetEvolutionChainByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new EvolutionChain { Id = 1 });

        var controller = new PokeApiController(Mock.Of<IMediator>());

        var result = await controller.GetEvolutionChain(1, catalog.Object, CancellationToken.None);

        result.Result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task GetEvolutionChainBySpecies_ReturnsNotFoundWhenMissing()
    {
        var mediator = new Mock<IMediator>();
        mediator.Setup(m => m.Send(It.IsAny<GetEvolutionChainBySpeciesNameQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((EvolutionChain?)null);

        var controller = new PokeApiController(mediator.Object);

        var result = await controller.GetEvolutionChainBySpecies("missing", CancellationToken.None);

        result.Result.Should().BeOfType<NotFoundResult>();
    }

    [Fact]
    public async Task GetLegendaryPokemonNames_ReturnsOk()
    {
        var catalog = new Mock<IPokemonCatalogService>();
        catalog.Setup(s => s.GetLegendaryPokemonNamesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new HashSet<string> { "mewtwo" });

        var controller = new PokeApiController(Mock.Of<IMediator>());

        var result = await controller.GetLegendaryPokemonNames(catalog.Object, CancellationToken.None);

        result.Result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task GetAllAbilities_ReturnsOk()
    {
        var mediator = new Mock<IMediator>();
        mediator.Setup(m => m.Send(It.IsAny<GetAllAbilitiesQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Ability> { new() { Id = 65, Name = "overgrow" } });

        var controller = new PokeApiController(mediator.Object);

        var result = await controller.GetAllAbilities(CancellationToken.None);

        result.Result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task GetAbilityByName_ReturnsOkWhenFound()
    {
        var mediator = new Mock<IMediator>();
        mediator.Setup(m => m.Send(It.IsAny<GetAbilityByNameQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Ability { Id = 65, Name = "overgrow" });

        var controller = new PokeApiController(mediator.Object);

        var result = await controller.GetAbilityByName("overgrow", CancellationToken.None);

        result.Result.Should().BeOfType<OkObjectResult>();
    }
}
