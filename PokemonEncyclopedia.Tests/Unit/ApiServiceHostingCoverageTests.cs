using Hangfire;
using Hangfire.InMemory;
using Hangfire.Storage;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using PokemonEncyclopedia.ApiService;
using PokemonEncyclopedia.ApiService.Services;

namespace PokemonEncyclopedia.Tests.Unit;

public class ApiServiceHostingCoverageTests
{
    [Fact]
    public async Task HangfireServerHostedService_StartsServerAndRegistersRecurringJob()
    {
        var storage = new InMemoryStorage();
        var services = new ServiceCollection();
        services.AddSingleton<JobStorage>(storage);
        await using var provider = services.BuildServiceProvider();

        var service = new HangfireServerHostedService(
            provider,
            Mock.Of<ILogger<HangfireServerHostedService>>());

        try
        {
            await service.StartAsync(CancellationToken.None);

            // The startup work runs on a background task; poll until the recurring
            // job registration is observable before stopping the service.
            var deadline = DateTime.UtcNow.AddSeconds(10);
            var registered = false;
            while (DateTime.UtcNow < deadline)
            {
                using (var probe = storage.GetConnection())
                {
                    if (probe.GetRecurringJobs().Any(job => job.Id == "pokemon-cache-refresh"))
                    {
                        registered = true;
                        break;
                    }
                }

                await Task.Delay(50);
            }

            registered.Should().BeTrue("the hosted service should register the recurring cache-refresh job");

            await service.StopAsync(CancellationToken.None);
        }
        finally
        {
            service.Dispose();
        }
    }

    [Fact]
    public async Task HangfireServerHostedService_StopWithoutStartDoesNotThrow()
    {
        var services = new ServiceCollection();
        services.AddSingleton<JobStorage>(new InMemoryStorage());
        await using var provider = services.BuildServiceProvider();

        var service = new HangfireServerHostedService(
            provider,
            Mock.Of<ILogger<HangfireServerHostedService>>());

        var act = async () =>
        {
            await service.StopAsync(CancellationToken.None);
            service.Dispose();
        };

        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task ConfigurePipeline_WiresMiddlewareInDevelopmentIntegrationTestMode()
    {
        var builder = WebApplication.CreateBuilder(new WebApplicationOptions
        {
            EnvironmentName = Environments.Development
        });
        builder.Configuration["ConnectionStrings:cache"] = "localhost:6379";

        // Avoid eager service construction so the in-proc build never opens
        // real Redis/telemetry connections while validating.
        builder.Host.UseDefaultServiceProvider(options =>
        {
            options.ValidateOnBuild = false;
            options.ValidateScopes = false;
        });

        ApiServiceStartup.ConfigureServices(builder, true);

        await using var app = builder.Build();

        var configure = () => ApiServiceStartup.ConfigurePipeline(app, true);

        configure.Should().NotThrow();
    }
}