using FluentValidation;
using FluentValidation.Results;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;
using PokeApiNet;
using PokemonEncyclopedia.ApiService.Controllers;
using PokemonEncyclopedia.ApiService.HealthChecks;
using PokemonEncyclopedia.ApiService.Middleware;
using PokemonEncyclopedia.ApiService.Services;
using PokemonEncyclopedia.Application.Features.GetAbilityByName;
using PokemonEncyclopedia.Application.Features.GetAllAbilities;
using PokemonEncyclopedia.Application.Features.GetAllPokemon;
using PokemonEncyclopedia.Application.Features.GetEvolutionChainBySpeciesName;
using PokemonEncyclopedia.Application.Features.GetMoveByName;
using PokemonEncyclopedia.Application.Features.GetPokemonByGeneration;
using PokemonEncyclopedia.Application.Features.GetPokemonByName;
using PokemonEncyclopedia.Application.Features.GetPokemonSpeciesByName;
using PokemonEncyclopedia.Application.Services;
using PokemonEncyclopedia.Infrastructure.Services;

namespace PokemonEncyclopedia.Tests.Unit;

public class ApiServiceCoverageTests
{
    [Fact]
    public async Task PokeApiController_ReturnsExpectedResults()
    {
        var pokemon = new[] { new Pokemon { Id = 1, Name = "bulbasaur" } };
        var pokemonSpecies = new[] { new PokemonSpecies { Id = 1, Name = "bulbasaur" } };
        var moves = new[] { new Move { Id = 1, Name = "tackle" } };
        var species = new PokemonSpecies { Id = 1, Name = "bulbasaur" };
        var ability = new Ability { Id = 65, Name = "overgrow" };
        var evolution = new EvolutionChain { Id = 1 };
        var catalog = new Mock<IPokemonCatalogService>();
        catalog.Setup(s => s.GetEvolutionChainByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(evolution);
        catalog.Setup(s => s.GetLegendaryPokemonNamesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new HashSet<string> { "mewtwo" });

        var mediator = new Mock<IMediator>();
        mediator.Setup(m => m.Send(It.IsAny<GetAllPokemonQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(pokemonSpecies);
        mediator.Setup(m => m.Send(It.IsAny<GetAllPokemonDetailsQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(pokemon);
        mediator.Setup(m => m.Send(It.IsAny<GetPokemonByNameQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(pokemon[0]);
        mediator.Setup(m => m.Send(It.IsAny<GetPokemonByGenerationQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<NamedApiResource<PokemonSpecies>>());
        mediator.Setup(m => m.Send(It.IsAny<GetMoveByNameQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(moves[0]);
        mediator.Setup(m => m.Send(It.IsAny<GetPokemonSpeciesByNameQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(species);
        mediator.Setup(m => m.Send(It.IsAny<GetEvolutionChainBySpeciesNameQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(evolution);
        mediator.Setup(m => m.Send(It.IsAny<GetAllAbilitiesQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { ability });
        mediator.Setup(m => m.Send(It.IsAny<GetAbilityByNameQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(ability);

        var controller = new PokeApiController(mediator.Object);

        (await controller.GetAllPokemon(CancellationToken.None)).Result.Should().BeOfType<OkObjectResult>();
        (await controller.GetAllPokemonDetails(CancellationToken.None)).Result.Should().BeOfType<OkObjectResult>();
        (await controller.GetPokemonByName("bulbasaur", CancellationToken.None)).Result.Should()
            .BeOfType<OkObjectResult>();
        (await controller.GetPokemonByGeneration(1, CancellationToken.None)).Result.Should().BeOfType<OkObjectResult>();
        (await controller.GetMoveByName("tackle", CancellationToken.None)).Result.Should().BeOfType<OkObjectResult>();
        (await controller.GetSpeciesByName("bulbasaur", CancellationToken.None)).Result.Should()
            .BeOfType<OkObjectResult>();
        (await controller.GetEvolutionChain(1, catalog.Object, CancellationToken.None)).Result.Should()
            .BeOfType<OkObjectResult>();
        (await controller.GetEvolutionChainBySpecies("bulbasaur", CancellationToken.None)).Result.Should()
            .BeOfType<OkObjectResult>();
        (await controller.GetLegendaryPokemonNames(catalog.Object, CancellationToken.None)).Result.Should()
            .BeOfType<OkObjectResult>();
        (await controller.GetAllAbilities(CancellationToken.None)).Result.Should().BeOfType<OkObjectResult>();
        (await controller.GetAbilityByName("overgrow", CancellationToken.None)).Result.Should()
            .BeOfType<OkObjectResult>();

        mediator.VerifyAll();
        catalog.VerifyAll();
    }

    [Fact]
    public async Task PokeApiController_ReturnsNotFoundForMissingResources()
    {
        var mediator = new Mock<IMediator>();
        var catalog = new Mock<IPokemonCatalogService>();
        mediator.Setup(m => m.Send(It.IsAny<GetPokemonByNameQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Pokemon?)null);
        mediator.Setup(m => m.Send(It.IsAny<GetMoveByNameQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Move?)null);
        mediator.Setup(m => m.Send(It.IsAny<GetPokemonSpeciesByNameQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((PokemonSpecies?)null);
        mediator.Setup(m => m.Send(It.IsAny<GetEvolutionChainBySpeciesNameQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((EvolutionChain?)null);
        mediator.Setup(m => m.Send(It.IsAny<GetAbilityByNameQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Ability?)null);
        catalog.Setup(s => s.GetEvolutionChainByIdAsync(0, It.IsAny<CancellationToken>()))
            .ReturnsAsync((EvolutionChain?)null);

        var controller = new PokeApiController(mediator.Object);

        (await controller.GetPokemonByName("missingno", CancellationToken.None)).Result.Should()
            .BeOfType<NotFoundResult>();
        (await controller.GetMoveByName("missing", CancellationToken.None)).Result.Should().BeOfType<NotFoundResult>();
        (await controller.GetSpeciesByName("missing", CancellationToken.None)).Result.Should()
            .BeOfType<NotFoundResult>();
        (await controller.GetEvolutionChainBySpecies("missing", CancellationToken.None)).Result.Should()
            .BeOfType<NotFoundResult>();
        (await controller.GetAbilityByName("missing", CancellationToken.None)).Result.Should()
            .BeOfType<NotFoundResult>();
    }

    [Fact]
    public async Task ApiExceptionMiddleware_WritesValidationProblemDetails()
    {
        var context = new DefaultHttpContext();
        context.Request.Method = "GET";
        context.Request.Path = "/api/pokemon/pikachu";
        context.Response.Body = new MemoryStream();

        var middleware = new ApiExceptionMiddleware(_ => throw new ValidationException(new[]
        {
            new ValidationFailure("name", "Name is required")
        }), Mock.Of<ILogger<ApiExceptionMiddleware>>());

        await middleware.InvokeAsync(context);

        context.Response.StatusCode.Should().Be(StatusCodes.Status400BadRequest);
        context.Response.ContentType.Should().StartWith("application/json");

        context.Response.Body.Position = 0;
        var body = await new StreamReader(context.Response.Body).ReadToEndAsync();
        body.Should().Contain("Validation failed");
        body.Should().Contain("Name is required");
    }

    [Fact]
    public async Task ApiExceptionMiddleware_WritesServerErrorProblemDetails()
    {
        var context = new DefaultHttpContext();
        context.Request.Method = "GET";
        context.Request.Path = "/api/pokemon/pikachu";
        context.Response.Body = new MemoryStream();

        var middleware = new ApiExceptionMiddleware(_ => throw new InvalidOperationException("boom"),
            Mock.Of<ILogger<ApiExceptionMiddleware>>());

        await middleware.InvokeAsync(context);

        context.Response.StatusCode.Should().Be(StatusCodes.Status500InternalServerError);
        context.Response.ContentType.Should().StartWith("application/json");
    }

    [Fact]
    public async Task ApiRequestLoggingMiddleware_InvokesNextAndCompletes()
    {
        var context = new DefaultHttpContext();
        context.Request.Method = "GET";
        context.Request.Path = "/api/pokemon/pikachu";
        context.Response.Body = new MemoryStream();
        var nextCalled = false;

        var middleware = new ApiRequestLoggingMiddleware(
            async ctx =>
            {
                nextCalled = true;
                ctx.Response.StatusCode = StatusCodes.Status204NoContent;
                await Task.CompletedTask;
            },
            Mock.Of<ILogger<ApiRequestLoggingMiddleware>>());

        await middleware.InvokeAsync(context);

        nextCalled.Should().BeTrue();
        context.Response.StatusCode.Should().Be(StatusCodes.Status204NoContent);
    }

    [Theory]
    [InlineData(true, null, HealthStatus.Healthy)]
    [InlineData(false, null, HealthStatus.Degraded)]
    [InlineData(false, "cache failed", HealthStatus.Unhealthy)]
    public async Task PokemonCatalogWarmupHealthCheck_ReturnsExpectedStatus(
        bool isWarm,
        string? errorMessage,
        HealthStatus expectedStatus)
    {
        var catalog = new Mock<IPokemonCatalogService>();
        catalog.SetupGet(s => s.IsWarm).Returns(isWarm);
        catalog.SetupGet(s => s.LastWarmupError)
            .Returns(errorMessage is null ? null : new InvalidOperationException(errorMessage));

        var healthCheck = new PokemonCatalogWarmupHealthCheck(catalog.Object);
        var result = await healthCheck.CheckHealthAsync(new HealthCheckContext());

        result.Status.Should().Be(expectedStatus);
    }

    [Fact]
    public async Task PokemonCatalogWarmupHostedService_CallsAllWarmupMethods()
    {
        var catalog = new Mock<IPokemonCatalogService>();
        catalog.Setup(s => s.GetAllPokemonAsync(It.IsAny<CancellationToken>())).ReturnsAsync(Array.Empty<Pokemon>());
        catalog.Setup(s => s.GetAllMovesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(Array.Empty<Move>());
        catalog.Setup(s => s.GetAllAbilitiesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(Array.Empty<Ability>());
        catalog.Setup(s => s.GetAllPokemonSpeciesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<PokemonSpecies>());

        var service =
            new TestPokemonCatalogWarmupHostedService(catalog.Object,
                Mock.Of<ILogger<PokemonCatalogWarmupHostedService>>());

        await service.RunAsync(CancellationToken.None);

        catalog.Verify(s => s.GetAllPokemonAsync(It.IsAny<CancellationToken>()), Times.Once);
        catalog.Verify(s => s.GetAllMovesAsync(It.IsAny<CancellationToken>()), Times.Once);
        catalog.Verify(s => s.GetAllAbilitiesAsync(It.IsAny<CancellationToken>()), Times.Once);
        catalog.Verify(s => s.GetAllPokemonSpeciesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task PokemonCatalogWarmupHostedService_SwallowsCancellation()
    {
        var catalog = new Mock<IPokemonCatalogService>();
        var cts = new CancellationTokenSource();
        cts.Cancel();
        catalog.Setup(s => s.GetAllPokemonAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new OperationCanceledException());

        var service =
            new TestPokemonCatalogWarmupHostedService(catalog.Object,
                Mock.Of<ILogger<PokemonCatalogWarmupHostedService>>());

        var act = () => service.RunAsync(cts.Token);

        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task PokemonCacheRefreshJob_CallsRefresh()
    {
        var catalog = new Mock<IPokemonCatalogService>();
        catalog.Setup(s => s.RefreshAllPokemonAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        var job = new PokemonCacheRefreshJob(catalog.Object, Mock.Of<ILogger<PokemonCacheRefreshJob>>());

        await job.Execute();

        catalog.Verify(s => s.RefreshAllPokemonAsync(CancellationToken.None), Times.Once);
    }

    [Fact]
    public async Task PokemonMovesRefreshJob_CallsRefresh()
    {
        var catalog = new Mock<IPokemonCatalogService>();
        catalog.Setup(s => s.GetAllMovesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(Array.Empty<Move>());
        var job = new PokemonMovesRefreshJob(catalog.Object, Mock.Of<ILogger<PokemonMovesRefreshJob>>());

        await job.Execute();

        catalog.Verify(s => s.GetAllMovesAsync(CancellationToken.None), Times.Once);
    }

    [Fact]
    public async Task PokemonSpeciesRefreshJob_CallsRefresh()
    {
        var catalog = new Mock<IPokemonCatalogService>();
        catalog.Setup(s => s.GetAllPokemonSpeciesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<PokemonSpecies>());
        var job = new PokemonSpeciesRefreshJob(catalog.Object, Mock.Of<ILogger<PokemonSpeciesRefreshJob>>());

        await job.Execute();

        catalog.Verify(s => s.GetAllPokemonSpeciesAsync(CancellationToken.None), Times.Once);
    }

    [Fact]
    public async Task PokemonLegendaryRefreshJob_CallsRefresh()
    {
        var catalog = new Mock<IPokemonCatalogService>();
        catalog.Setup(s => s.GetLegendaryPokemonNamesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new HashSet<string>());
        var job = new PokemonLegendaryRefreshJob(catalog.Object, Mock.Of<ILogger<PokemonLegendaryRefreshJob>>());

        await job.Execute();

        catalog.Verify(s => s.GetLegendaryPokemonNamesAsync(CancellationToken.None), Times.Once);
    }

    private sealed class TestPokemonCatalogWarmupHostedService(
        IPokemonCatalogService catalogService,
        ILogger<PokemonCatalogWarmupHostedService> logger)
        : PokemonCatalogWarmupHostedService(catalogService, logger)
    {
        public Task RunAsync(CancellationToken cancellationToken) => ExecuteAsync(cancellationToken);
    }
}