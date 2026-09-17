using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using PokeApiNet;
using PokemonEncyclopedia.ApiService;
using PokemonEncyclopedia.ApiService.Controllers;
using PokemonEncyclopedia.ApiService.Middleware;
using PokemonEncyclopedia.ApiService.Services;
using PokemonEncyclopedia.Application.Services;

namespace PokemonEncyclopedia.Tests.Unit;

public class ApiServiceEdgeCoverageTests
{
    [Fact]
    public async Task PokeApiController_GetEvolutionChain_ReturnsNotFoundWhenMissing()
    {
        var catalog = new Mock<IPokemonCatalogService>();
        catalog.Setup(s => s.GetEvolutionChainByIdAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((EvolutionChain?)null);

        var controller = new PokeApiController(Mock.Of<IMediator>());

        var result = await controller.GetEvolutionChain(999, catalog.Object, CancellationToken.None);

        result.Result.Should().BeOfType<NotFoundResult>();
    }

    [Fact]
    public async Task ApiExceptionMiddleware_PassesThroughWhenNoException()
    {
        var context = new DefaultHttpContext();
        var nextCalled = false;

        var middleware = new ApiExceptionMiddleware(
            _ =>
            {
                nextCalled = true;
                return Task.CompletedTask;
            },
            Mock.Of<ILogger<ApiExceptionMiddleware>>());

        await middleware.InvokeAsync(context);

        nextCalled.Should().BeTrue();
        context.Response.StatusCode.Should().Be(StatusCodes.Status200OK);
    }

    [Fact]
    public async Task PokemonCatalogWarmupHostedService_LogsAndSwallowsUnexpectedError()
    {
        var catalog = new Mock<IPokemonCatalogService>();
        catalog.Setup(s => s.GetAllPokemonAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("boom"));

        var service = new TestWarmupHostedService(
            catalog.Object,
            Mock.Of<ILogger<PokemonCatalogWarmupHostedService>>());

        var act = () => service.RunAsync(CancellationToken.None);

        await act.Should().NotThrowAsync();
        catalog.Verify(s => s.GetAllPokemonAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public void CreateCosmosClientOptions_HttpClientFactoryProducesClient()
    {
        var environment = Mock.Of<IHostEnvironment>(h => h.EnvironmentName == Environments.Production);

        var options = ApiServiceStartup.CreateCosmosClientOptions(
            environment,
            new Uri("https://cosmos.example.com/"),
            true);

        options.HttpClientFactory.Should().NotBeNull();
        using var client = options.HttpClientFactory!();
        client.Should().NotBeNull();
    }

    private sealed class TestWarmupHostedService(
        IPokemonCatalogService catalogService,
        ILogger<PokemonCatalogWarmupHostedService> logger)
        : PokemonCatalogWarmupHostedService(catalogService, logger)
    {
        public Task RunAsync(CancellationToken cancellationToken)
        {
            return ExecuteAsync(cancellationToken);
        }
    }
}