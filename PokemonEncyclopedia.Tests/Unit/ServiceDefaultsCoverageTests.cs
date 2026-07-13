using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Hosting;
using PokemonEncyclopedia.ServiceDefaults;

namespace PokemonEncyclopedia.Tests.Unit;

public class ServiceDefaultsCoverageTests
{
    [Fact]
    public void AddServiceDefaults_RegistersHealthChecksAndHttpClientDefaults()
    {
        var builder = Host.CreateApplicationBuilder(new HostApplicationBuilderSettings
        {
            EnvironmentName = Environments.Development
        });

        builder.AddServiceDefaults();

        using var provider = builder.Build();

        provider.Services.GetRequiredService<HealthCheckService>().Should().NotBeNull();
        provider.Services.GetRequiredService<IHttpClientFactory>().Should().NotBeNull();
    }

    [Fact]
    public void MapDefaultEndpoints_MapsHealthEndpointsInDevelopment()
    {
        var builder = WebApplication.CreateBuilder(new WebApplicationOptions
        {
            EnvironmentName = Environments.Development
        });
        builder.Services.AddHealthChecks();

        var app = builder.Build();
        app.MapDefaultEndpoints();
    }

    [Fact]
    public void MapDefaultEndpoints_DoesNotMapHealthEndpointsOutsideDevelopment()
    {
        var builder = WebApplication.CreateBuilder(new WebApplicationOptions
        {
            EnvironmentName = Environments.Production
        });
        builder.Services.AddHealthChecks();

        var app = builder.Build();
        app.MapDefaultEndpoints();
    }
}
