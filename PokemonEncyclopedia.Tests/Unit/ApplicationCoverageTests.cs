using FluentValidation;
using MediatR;
using Microsoft.Extensions.Logging;
using PokemonEncyclopedia.Application.Features.GetAbilityByName;
using PokemonEncyclopedia.Application.Features.GetAllAbilities;
using PokemonEncyclopedia.Application.Features.GetAllPokemon;
using PokemonEncyclopedia.Application.Features.GetEvolutionChainBySpeciesName;
using PokemonEncyclopedia.Application.Features.GetMoveByName;
using PokemonEncyclopedia.Application.Features.GetPokemonByGeneration;
using PokemonEncyclopedia.Application.Features.GetPokemonByName;
using PokemonEncyclopedia.Application.Features.GetPokemonSpeciesByName;
using PokemonEncyclopedia.Application.Services;
using PokemonEncyclopedia.Application.Validators.Behavior;
using PokeApiNet;

namespace PokemonEncyclopedia.Tests.Unit;

public class ApplicationCoverageTests
{
    [Fact]
    public async Task GetAllPokemonHandler_ReturnsSpeciesFromCatalog()
    {
        var catalog = new Mock<IPokemonCatalogService>();
        var expected = new List<PokemonSpecies> { new() { Id = 1, Name = "bulbasaur" } };
        catalog.Setup(s => s.GetAllPokemonSpeciesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(expected);

        var handler = new GetAllPokemonHandler(catalog.Object, Mock.Of<ILogger<GetAllPokemonHandler>>());

        var result = await handler.Handle(new GetAllPokemonQuery(), CancellationToken.None);

        result.Should().BeEquivalentTo(expected);
    }

    [Fact]
    public async Task GetAllPokemonDetailsHandler_ReturnsPokemonFromCatalog()
    {
        var catalog = new Mock<IPokemonCatalogService>();
        var expected = new List<Pokemon> { new() { Id = 25, Name = "pikachu" } };
        catalog.Setup(s => s.GetAllPokemonAsync(It.IsAny<CancellationToken>())).ReturnsAsync(expected);

        var handler = new GetAllPokemonDetailsHandler(catalog.Object, Mock.Of<ILogger<GetAllPokemonDetailsHandler>>());

        var result = await handler.Handle(new GetAllPokemonDetailsQuery(), CancellationToken.None);

        result.Should().BeEquivalentTo(expected);
    }

    [Fact]
    public async Task GetAllAbilitiesHandler_ReturnsAbilitiesFromCatalog()
    {
        var catalog = new Mock<IPokemonCatalogService>();
        var expected = new List<Ability> { new() { Id = 65, Name = "overgrow" } };
        catalog.Setup(s => s.GetAllAbilitiesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(expected);

        var handler = new GetAllAbilitiesHandler(catalog.Object, Mock.Of<ILogger<GetAllAbilitiesHandler>>());

        var result = await handler.Handle(new GetAllAbilitiesQuery(), CancellationToken.None);

        result.Should().BeEquivalentTo(expected);
    }

    [Fact]
    public async Task GetAbilityByNameHandler_ReturnsAbilityFromCatalog()
    {
        var catalog = new Mock<IPokemonCatalogService>();
        var expected = new Ability { Id = 65, Name = "overgrow" };
        catalog.Setup(s => s.GetAbilityByNameAsync("overgrow", It.IsAny<CancellationToken>())).ReturnsAsync(expected);

        var handler = new GetAbilityByNameHandler(catalog.Object, Mock.Of<ILogger<GetAbilityByNameHandler>>());

        var result = await handler.Handle(new GetAbilityByNameQuery("overgrow"), CancellationToken.None);

        result.Should().BeEquivalentTo(expected);
    }

    [Fact]
    public async Task GetPokemonSpeciesByNameHandler_ReturnsSpeciesFromCatalog()
    {
        var catalog = new Mock<IPokemonCatalogService>();
        var expected = new PokemonSpecies { Id = 1, Name = "bulbasaur" };
        catalog.Setup(s => s.GetPokemonSpeciesByNameAsync("bulbasaur", It.IsAny<CancellationToken>())).ReturnsAsync(expected);

        var handler = new GetPokemonSpeciesByNameHandler(catalog.Object, Mock.Of<ILogger<GetPokemonSpeciesByNameHandler>>());

        var result = await handler.Handle(new GetPokemonSpeciesByNameQuery("bulbasaur"), CancellationToken.None);

        result.Should().BeEquivalentTo(expected);
    }

    [Fact]
    public async Task GetEvolutionChainBySpeciesNameHandler_ReturnsChainFromCatalog()
    {
        var catalog = new Mock<IPokemonCatalogService>();
        var expected = new EvolutionChain { Id = 1 };
        catalog.Setup(s => s.GetEvolutionChainBySpeciesNameAsync("bulbasaur", It.IsAny<CancellationToken>())).ReturnsAsync(expected);

        var handler = new GetEvolutionChainBySpeciesNameHandler(catalog.Object, Mock.Of<ILogger<GetEvolutionChainBySpeciesNameHandler>>());

        var result = await handler.Handle(new GetEvolutionChainBySpeciesNameQuery("bulbasaur"), CancellationToken.None);

        result.Should().BeEquivalentTo(expected);
    }

    [Fact]
    public async Task GetPokemonByGenerationHandler_ReturnsSpeciesResourcesFromCatalog()
    {
        var catalog = new Mock<IPokemonCatalogService>();
        var expected = new List<NamedApiResource<PokemonSpecies>>
        {
            new() { Name = "bulbasaur", Url = "https://pokeapi.co/api/v2/pokemon-species/1/" }
        };
        catalog.Setup(s => s.GetPokemonSpeciesByGenerationAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(expected);

        var handler = new GetPokemonByGenerationHandler(catalog.Object, Mock.Of<ILogger<GetPokemonByGenerationHandler>>());

        var result = await handler.Handle(new GetPokemonByGenerationQuery(1), CancellationToken.None);

        result.Should().BeEquivalentTo(expected);
    }

    [Fact]
    public async Task GetMoveByNameHandler_ReturnsMoveFromCatalog()
    {
        var catalog = new Mock<IPokemonCatalogService>();
        var expected = new Move { Id = 15, Name = "cut" };
        catalog.Setup(s => s.GetMoveByNameAsync("cut", It.IsAny<CancellationToken>())).ReturnsAsync(expected);

        var handler = new GetMoveByNameHandler(catalog.Object, Mock.Of<ILogger<GetMoveByNameHandler>>());

        var result = await handler.Handle(new GetMoveByNameQuery("cut"), CancellationToken.None);

        result.Should().BeEquivalentTo(expected);
    }

    [Fact]
    public async Task GetPokemonByNameHandler_ReturnsPokemonFromCatalog()
    {
        var catalog = new Mock<IPokemonCatalogService>();
        var expected = new Pokemon { Id = 25, Name = "pikachu" };
        catalog.Setup(s => s.GetPokemonByNameAsync("pikachu", It.IsAny<CancellationToken>())).ReturnsAsync(expected);

        var handler = new GetPokemonByNameHandler(catalog.Object, Mock.Of<ILogger<GetPokemonByNameHandler>>());

        var result = await handler.Handle(new GetPokemonByNameQuery("pikachu"), CancellationToken.None);

        result.Should().BeEquivalentTo(expected);
    }

    [Fact]
    public async Task GetPokemonByNameHandler_ReturnsNullWhenCatalogMisses()
    {
        var catalog = new Mock<IPokemonCatalogService>();
        catalog.Setup(s => s.GetPokemonByNameAsync("missingno", It.IsAny<CancellationToken>())).ReturnsAsync((Pokemon?)null);

        var handler = new GetPokemonByNameHandler(catalog.Object, Mock.Of<ILogger<GetPokemonByNameHandler>>());

        var result = await handler.Handle(new GetPokemonByNameQuery("missingno"), CancellationToken.None);

        result.Should().BeNull();
    }

    [Fact]
    public async Task ValidationBehavior_ReturnsNextWhenNoValidators()
    {
        var behavior = new ValidationBehavior<GetPokemonByNameQuery, Pokemon?>(
            Array.Empty<IValidator<GetPokemonByNameQuery>>(),
            Mock.Of<ILogger<ValidationBehavior<GetPokemonByNameQuery, Pokemon?>>>());

        var nextCalled = false;
        var result = await behavior.Handle(
            new GetPokemonByNameQuery("pikachu"),
            () =>
            {
                nextCalled = true;
                return Task.FromResult<Pokemon?>(new Pokemon { Id = 25, Name = "pikachu" });
            },
            CancellationToken.None);

        nextCalled.Should().BeTrue();
        result.Should().NotBeNull();
        result!.Name.Should().Be("pikachu");
    }

    [Fact]
    public async Task ValidationBehavior_ThrowsWhenValidationFails()
    {
        var validator = new InlineValidator<GetPokemonByNameQuery>();
        validator.RuleFor(x => x.Name).NotEmpty();

        var behavior = new ValidationBehavior<GetPokemonByNameQuery, Pokemon?>(
            [validator],
            Mock.Of<ILogger<ValidationBehavior<GetPokemonByNameQuery, Pokemon?>>>());

        var act = () => behavior.Handle(new GetPokemonByNameQuery(""), () => Task.FromResult<Pokemon?>(null), CancellationToken.None);

        await act.Should().ThrowAsync<ValidationException>();
    }
}
