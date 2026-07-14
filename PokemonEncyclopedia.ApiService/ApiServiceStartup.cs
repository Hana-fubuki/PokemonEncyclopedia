using System.Data.Common;
using a2n.Hangfire.Dashboard;
using Hangfire;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Cosmos;
using Microsoft.OpenApi;
using PokemonEncyclopedia.ApiService.HealthChecks;
using PokemonEncyclopedia.ApiService.Middleware;
using PokemonEncyclopedia.ApiService.Services;
using PokemonEncyclopedia.Application.DependencyInjection;
using PokemonEncyclopedia.Infrastructure.DependencyInjection;
using Scalar.AspNetCore;
using HealthStatus = Microsoft.Extensions.Diagnostics.HealthChecks.HealthStatus;

namespace PokemonEncyclopedia.ApiService;

public static class ApiServiceStartup
{
    public const string HangfireCollectionName = "hangfire";

    public static bool IsIntegrationTestMode(IConfiguration configuration)
    {
        var deploymentMode = configuration["DEPLOYMENT_MODE"] ?? "local";
        return deploymentMode.Equals("test", StringComparison.OrdinalIgnoreCase) ||
               string.Equals(configuration["INTEGRATION_TEST_MODE"], "true", StringComparison.OrdinalIgnoreCase);
    }

    public static void ConfigureServices(WebApplicationBuilder builder, bool isIntegrationTestMode)
    {
        builder.Services.AddApplicationInsightsTelemetry();
        builder.AddServiceDefaults();

        // In integration test mode, use in-memory distributed cache to avoid Docker container dependencies.
        if (isIntegrationTestMode)
            builder.Services.AddDistributedMemoryCache();
        else
            builder.AddRedisDistributedCache("cache");

        builder.Services.AddApplicationServices();
        builder.Services.AddInfrastructureServices();

        builder.Services.AddProblemDetails();
        builder.Services.AddControllers();
        builder.Services.AddEndpointsApiExplorer();
        ConfigureApiBehavior(builder.Services);
        ConfigureOpenApi(builder.Services);
        ConfigureSwagger(builder.Services);

        // In integration test mode, skip the catalog warmup health check so the health endpoint
        // returns 200 immediately (the warmup service does not run in integration test mode).
        if (!isIntegrationTestMode)
            builder.Services.AddHealthChecks()
                .AddCheck<PokemonCatalogWarmupHealthCheck>("pokemon_catalog_warmup");
        else
            builder.Services.AddHealthChecks();

        if (isIntegrationTestMode)
            return;

        builder.Services.AddHostedService<PokemonCatalogWarmupHostedService>();
        builder.Services.AddHangfireDashboardUI();

        var hangfireSettings = ResolveHangfireCosmosSettings(builder.Configuration);
        var cosmosClientOptions = CreateCosmosClientOptions(builder.Environment, hangfireSettings.Endpoint,
            hangfireSettings.DisableServerCertificateValidation);

        builder.Services.AddHangfire(config => config
            .UseSimpleAssemblyNameTypeSerializer()
            .UseRecommendedSerializerSettings()
            .UseAzureCosmosDbStorage(
                hangfireSettings.Endpoint.AbsoluteUri,
                hangfireSettings.AuthSecret,
                hangfireSettings.DatabaseName,
                HangfireCollectionName,
                cosmosClientOptions));
        builder.Services.AddHostedService<HangfireServerHostedService>();
    }

    public static void ConfigurePipeline(WebApplication app, bool isIntegrationTestMode)
    {
        var logger = app.Services.GetRequiredService<ILoggerFactory>().CreateLogger("PokemonEncyclopedia.ApiService");
        logger.LogInformation("Starting Pokémon API service");

        app.UseMiddleware<ApiRequestLoggingMiddleware>();
        app.UseMiddleware<ApiExceptionMiddleware>();

        app.MapOpenApi();

        if (app.Environment.IsDevelopment())
        {
            logger.LogInformation("Development environment detected. Enabling Swagger UI and Scalar UI");
            app.UseSwagger();
            app.UseSwaggerUI(options =>
            {
                options.SwaggerEndpoint("/swagger/v1/swagger.json", "Pokémon API v1");
                options.RoutePrefix = "swagger";
                options.DisplayRequestDuration();
                options.DefaultModelsExpandDepth(2);
            });

            app.MapScalarApiReference(options =>
            {
                options.WithTitle("Pokémon Encyclopedia API");
                options.WithTheme(ScalarTheme.Kepler);
                options.WithDefaultHttpClient(ScalarTarget.CSharp, ScalarClient.HttpClient);
                options.DotNetFlag = true;
            });

            app.MapHealthChecks("/health", new HealthCheckOptions
            {
                ResultStatusCodes =
                {
                    [HealthStatus.Healthy] = StatusCodes.Status200OK,
                    [HealthStatus.Degraded] = StatusCodes.Status503ServiceUnavailable,
                    [HealthStatus.Unhealthy] = StatusCodes.Status503ServiceUnavailable
                }
            });

            app.MapHealthChecks("/alive", new HealthCheckOptions
            {
                Predicate = r => r.Tags.Contains("live")
            });
        }

        app.Lifetime.ApplicationStarted.Register(() =>
        {
            logger.LogInformation("🚀 Pokémon API started successfully!");
            foreach (var address in app.Urls)
                logger.LogInformation("   Base URL: {BaseUrl}", address);
        });

        app.UseHttpsRedirection();
        app.UseAuthorization();

        if (!isIntegrationTestMode)
            app.UseHangfireDashboardUI("/hangfire", new DashboardUIOptions
            {
                DashboardTitle = "Pokémon Encyclopedia Jobs",
                DefaultTheme = "auto",
                EnableJobManagement = true,
                Authorization = []
            });

        logger.LogInformation("Mapping controllers and default endpoints");
        app.MapControllers();
        logger.LogInformation("Pokémon API service started successfully");
    }

    public static HangfireCosmosSettings ResolveHangfireCosmosSettings(IConfiguration configuration)
    {
        var cosmosConnectionString =
            configuration.GetConnectionString("hangfiredb") ?? configuration["HANGFIREDB_CONNECTIONSTRING"];
        var cosmosUri = configuration["HANGFIREDB_URI"];
        var cosmosAuthSecret = configuration["HANGFIREDB_ACCOUNTKEY"];
        var cosmosDatabaseName = configuration["HANGFIREDB_DATABASENAME"] ?? "hangfiredb";
        var disableServerCertificateValidation = false;

        if (!string.IsNullOrWhiteSpace(cosmosConnectionString))
        {
            var csb = new DbConnectionStringBuilder { ConnectionString = cosmosConnectionString };
            if (csb.TryGetValue("AccountEndpoint", out var endpointValue))
                cosmosUri = endpointValue?.ToString();
            if (csb.TryGetValue("AccountKey", out var keyValue))
                cosmosAuthSecret = keyValue?.ToString();
            if (csb.TryGetValue("DisableServerCertificateValidation", out var disableCertValue) &&
                bool.TryParse(disableCertValue?.ToString(), out var disableCertValidation))
                disableServerCertificateValidation = disableCertValidation;
        }

        if (string.IsNullOrWhiteSpace(cosmosUri) || string.IsNullOrWhiteSpace(cosmosAuthSecret))
            throw new InvalidOperationException(
                "Cosmos DB configuration for Hangfire is missing. Ensure AppHost references the 'hangfiredb' Cosmos resource.");

        if (!Uri.TryCreate(cosmosUri, UriKind.Absolute, out var cosmosEndpoint) ||
            (cosmosEndpoint.Scheme != Uri.UriSchemeHttps && cosmosEndpoint.Scheme != Uri.UriSchemeHttp))
            throw new InvalidOperationException(
                $"Invalid Cosmos endpoint '{cosmosUri}'. Expected an http/https AccountEndpoint value.");

        return new HangfireCosmosSettings(cosmosEndpoint, cosmosAuthSecret, cosmosDatabaseName,
            disableServerCertificateValidation);
    }

    public static CosmosClientOptions CreateCosmosClientOptions(
        IHostEnvironment environment,
        Uri cosmosEndpoint,
        bool disableServerCertificateValidation)
    {
        var cosmosClientOptions = new CosmosClientOptions();

        if (environment.IsDevelopment() && IsLocalCosmosEmulatorEndpoint(cosmosEndpoint))
        {
            cosmosClientOptions.ConnectionMode = ConnectionMode.Gateway;
            cosmosClientOptions.LimitToEndpoint = true;
        }

        if (disableServerCertificateValidation ||
            (environment.IsDevelopment() && IsLocalCosmosEmulatorEndpoint(cosmosEndpoint)))
            cosmosClientOptions.HttpClientFactory = () =>
            {
                var handler = new HttpClientHandler
                {
                    ServerCertificateCustomValidationCallback =
                        HttpClientHandler.DangerousAcceptAnyServerCertificateValidator
                };
                return new HttpClient(handler, true);
            };

        return cosmosClientOptions;
    }

    public static bool IsLocalCosmosEmulatorEndpoint(Uri endpoint)
    {
        return endpoint.Scheme == Uri.UriSchemeHttps &&
               (endpoint.Host.Equals("localhost", StringComparison.OrdinalIgnoreCase) ||
                endpoint.Host.Equals("127.0.0.1", StringComparison.OrdinalIgnoreCase) ||
                endpoint.Host.Equals("::1", StringComparison.OrdinalIgnoreCase));
    }

    private static void ConfigureApiBehavior(IServiceCollection services)
    {
        services.Configure<ApiBehaviorOptions>(options =>
        {
            options.InvalidModelStateResponseFactory = context =>
            {
                var errors = context.ModelState
                    .Where(kvp => kvp.Value?.Errors.Count > 0)
                    .ToDictionary(
                        kvp => kvp.Key,
                        kvp => kvp.Value!.Errors.Select(e => e.ErrorMessage).ToArray());

                return new BadRequestObjectResult(new ValidationProblemDetails(errors)
                {
                    Title = "Validation failed",
                    Status = StatusCodes.Status400BadRequest,
                    Detail = "One or more validation rules failed."
                });
            };
        });
    }

    private static void ConfigureOpenApi(IServiceCollection services)
    {
        services.AddOpenApi(options =>
        {
            options.AddDocumentTransformer((document, _, _) =>
            {
                document.Info.Title = "Pokémon Encyclopedia API";
                document.Info.Version = "v1";
                document.Info.Description = "API for retrieving comprehensive Pokémon data from the PokéAPI service";
                document.Info.Contact = new OpenApiContact
                {
                    Name = "Pokémon Encyclopedia",
                    Url = new Uri("https://github.com/Hana-fubuki/PokemonEncyclopedia")
                };
                document.Info.License = new OpenApiLicense
                {
                    Name = "MIT",
                    Url = new Uri("https://opensource.org/licenses/MIT")
                };
                return Task.CompletedTask;
            });
        });
    }

    private static void ConfigureSwagger(IServiceCollection services)
    {
        services.AddSwaggerGen(options =>
        {
            options.CustomSchemaIds(type => (type.FullName ?? type.Name).Replace('+', '.'));
            options.EnableAnnotations();
            options.SwaggerDoc("v1", new OpenApiInfo
            {
                Title = "Pokémon API",
                Version = "v1",
                Description = "Swagger documentation for the Pokémon Encyclopedia API"
            });
        });
    }

    public sealed record HangfireCosmosSettings(
        Uri Endpoint,
        string AuthSecret,
        string DatabaseName,
        bool DisableServerCertificateValidation);
}