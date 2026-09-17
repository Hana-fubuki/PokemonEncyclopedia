using FluentValidation;
using FluentValidation.Results;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using PokemonEncyclopedia.ApiService;
using PokemonEncyclopedia.ApiService.Middleware;

namespace PokemonEncyclopedia.Tests.Unit;

public class ApiServiceStartupCoverageTests
{
    [Theory]
    [InlineData("test", null, true)]
    [InlineData("TEST", null, true)]
    [InlineData("local", "true", true)]
    [InlineData("local", "TRUE", true)]
    [InlineData("local", null, false)]
    [InlineData(null, null, false)]
    public void IsIntegrationTestMode_HonorsBothSignals(
        string? deploymentMode,
        string? integrationTestMode,
        bool expected)
    {
        var settings = new Dictionary<string, string?>();
        if (deploymentMode is not null)
            settings["DEPLOYMENT_MODE"] = deploymentMode;
        if (integrationTestMode is not null)
            settings["INTEGRATION_TEST_MODE"] = integrationTestMode;

        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(settings)
            .Build();

        ApiServiceStartup.IsIntegrationTestMode(configuration).Should().Be(expected);
    }

    [Fact]
    public void ResolveHangfireCosmosSettings_UsesUriAndAccountKeyWithDefaultDatabase()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["HANGFIREDB_URI"] = "https://cosmos.example.com/",
                ["HANGFIREDB_ACCOUNTKEY"] = "abc123"
            })
            .Build();

        var settings = ApiServiceStartup.ResolveHangfireCosmosSettings(configuration);

        settings.Endpoint.Should().Be(new Uri("https://cosmos.example.com/"));
        settings.AuthSecret.Should().Be("abc123");
        settings.DatabaseName.Should().Be("hangfiredb");
        settings.DisableServerCertificateValidation.Should().BeFalse();
    }

    [Fact]
    public void ResolveHangfireCosmosSettings_PrefersConnectionStringsSection()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:hangfiredb"] =
                    "AccountEndpoint=https://from-section:8081/;AccountKey=sectionkey",
                ["HANGFIREDB_DATABASENAME"] = "jobs"
            })
            .Build();

        var settings = ApiServiceStartup.ResolveHangfireCosmosSettings(configuration);

        settings.Endpoint.Should().Be(new Uri("https://from-section:8081/"));
        settings.AuthSecret.Should().Be("sectionkey");
        settings.DatabaseName.Should().Be("jobs");
    }

    [Fact]
    public void ResolveHangfireCosmosSettings_ThrowsForNonHttpEndpoint()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["HANGFIREDB_CONNECTIONSTRING"] = "AccountEndpoint=ftp://cosmos/;AccountKey=secret"
            })
            .Build();

        var act = () => ApiServiceStartup.ResolveHangfireCosmosSettings(configuration);

        act.Should().Throw<InvalidOperationException>().WithMessage("*Invalid Cosmos endpoint*");
    }

    [Fact]
    public void CreateCosmosClientOptions_ProductionEndpointUsesDefaults()
    {
        var environment = Mock.Of<IHostEnvironment>(h => h.EnvironmentName == Environments.Production);

        var options = ApiServiceStartup.CreateCosmosClientOptions(
            environment,
            new Uri("https://cosmos.example.com/"),
            false);

        options.ConnectionMode.Should().Be(ConnectionMode.Direct);
        options.LimitToEndpoint.Should().BeFalse();
        options.HttpClientFactory.Should().BeNull();
    }

    [Fact]
    public void CreateCosmosClientOptions_DisableCertValidationOverridesHttpFactory()
    {
        var environment = Mock.Of<IHostEnvironment>(h => h.EnvironmentName == Environments.Production);

        var options = ApiServiceStartup.CreateCosmosClientOptions(
            environment,
            new Uri("https://cosmos.example.com/"),
            true);

        options.ConnectionMode.Should().Be(ConnectionMode.Direct);
        options.LimitToEndpoint.Should().BeFalse();
        options.HttpClientFactory.Should().NotBeNull();
    }

    [Theory]
    [InlineData("https://localhost:8081/", true)]
    [InlineData("https://LOCALHOST:8081/", true)]
    [InlineData("https://127.0.0.1:8081/", true)]
    [InlineData("https://cosmos.example.com/", false)]
    [InlineData("http://localhost:8081/", false)]
    public void IsLocalCosmosEmulatorEndpoint_DetectsLoopbackHttps(string uri, bool expected)
    {
        ApiServiceStartup.IsLocalCosmosEmulatorEndpoint(new Uri(uri)).Should().Be(expected);
    }

    [Fact]
    public async Task ApiExceptionMiddleware_RethrowsValidationExceptionWhenResponseStarted()
    {
        var context = new DefaultHttpContext();
        context.Features.Set<IHttpResponseFeature>(new StartedResponseFeature());

        var middleware = new ApiExceptionMiddleware(
            _ => throw new ValidationException(new[] { new ValidationFailure("name", "required") }),
            Mock.Of<ILogger<ApiExceptionMiddleware>>());

        var act = () => middleware.InvokeAsync(context);

        await act.Should().ThrowAsync<ValidationException>();
    }

    [Fact]
    public async Task ApiExceptionMiddleware_RethrowsServerErrorWhenResponseStarted()
    {
        var context = new DefaultHttpContext();
        context.Features.Set<IHttpResponseFeature>(new StartedResponseFeature());

        var middleware = new ApiExceptionMiddleware(
            _ => throw new InvalidOperationException("boom"),
            Mock.Of<ILogger<ApiExceptionMiddleware>>());

        var act = () => middleware.InvokeAsync(context);

        await act.Should().ThrowAsync<InvalidOperationException>();
    }

    private sealed class StartedResponseFeature : IHttpResponseFeature
    {
        public Stream Body { get; set; } = Stream.Null;
        public bool HasStarted => true;
        public IHeaderDictionary Headers { get; set; } = new HeaderDictionary();
        public string? ReasonPhrase { get; set; }
        public int StatusCode { get; set; } = StatusCodes.Status200OK;

        public void OnCompleted(Func<object, Task> callback, object state)
        {
        }

        public void OnStarting(Func<object, Task> callback, object state)
        {
        }
    }
}