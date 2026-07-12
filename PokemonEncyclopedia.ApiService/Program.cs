using System.Data.Common;
using System.Reflection;
using FluentValidation;
using Hangfire;
using MediatR;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.OpenApi;
using PokeApiNet;
using a2n.Hangfire.Dashboard;
using Aspire.Seq;
using PokemonEncyclopedia.ApiService.HealthChecks;
using PokemonEncyclopedia.ApiService.Middleware;
using PokemonEncyclopedia.ApiService.Services;
using PokemonEncyclopedia.ApiService.Swagger;
using PokemonEncyclopedia.ApiService.Validators;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);
var assembly = Assembly.GetExecutingAssembly();

// ------------------------------------------------------------
// Service Defaults & Core Services
// ------------------------------------------------------------
builder.AddServiceDefaults();
builder.AddRedisDistributedCache("cache");

builder.Services.AddProblemDetails();
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

// MediatR and validation pipeline
builder.Services.AddMediatR(assembly);
builder.Services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));

// FluentValidation integration and registration
builder.Services.AddValidatorsFromAssemblyContaining<GetPokemonByGenerationQueryValidator>();

// Consistent 400 response for MVC model binding failures
builder.Services.Configure<ApiBehaviorOptions>(options =>
{
    options.InvalidModelStateResponseFactory = context =>
    {
        var errors = context.ModelState
            .Where(kvp => kvp.Value?.Errors.Count > 0)
            .Select(kvp => new { Field = kvp.Key, Errors = kvp.Value?.Errors.Select(e => e.ErrorMessage) });

        return new BadRequestObjectResult(new { message = "Validation failed", errors });
    };
});

// OpenAPI / Aspire OpenAPI
builder.Services.AddOpenApi(options =>
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

// Swagger (Swashbuckle)
builder.Services.AddSwaggerGen(options =>
{
    options.CustomSchemaIds(type => (type.FullName ?? type.Name).Replace('+', '.'));
    options.DocumentFilter<PokemonSpritesSchemaAliasDocumentFilter>();
    options.EnableAnnotations();
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Pokémon API",
        Version = "v1",
        Description = "Swagger documentation for the Pokémon Encyclopedia API"
    });
});

// PokeApi client
builder.Services.AddSingleton<PokeApiClient>();
builder.Services.AddSingleton<IPokemonCatalogService, PokemonCatalogService>();
builder.Services.AddTransient<PokemonCacheRefreshJob>();
builder.Services.AddHostedService<PokemonCatalogWarmupHostedService>();
builder.Services.AddHealthChecks()
    .AddCheck<PokemonCatalogWarmupHealthCheck>("pokemon_catalog_warmup");
builder.AddSeqEndpoint("seq");
builder.Services.AddHangfireDashboardUI();

var cosmosConnectionString =
    builder.Configuration.GetConnectionString("hangfiredb") ?? builder.Configuration["HANGFIREDB_CONNECTIONSTRING"];
var cosmosUri = builder.Configuration["HANGFIREDB_URI"];
var cosmosAuthSecret = builder.Configuration["HANGFIREDB_ACCOUNTKEY"];
var cosmosDatabaseName = builder.Configuration["HANGFIREDB_DATABASENAME"] ?? "hangfiredb";
var disableServerCertificateValidation = false;
const string hangfireCollectionName = "hangfire";

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

var cosmosClientOptions = new CosmosClientOptions();
if (builder.Environment.IsDevelopment() && IsLocalCosmosEmulatorEndpoint(cosmosEndpoint))
{
    cosmosClientOptions.ConnectionMode = ConnectionMode.Gateway;
    cosmosClientOptions.LimitToEndpoint = true;
}

if (disableServerCertificateValidation ||
    (builder.Environment.IsDevelopment() && IsLocalCosmosEmulatorEndpoint(cosmosEndpoint)))
    cosmosClientOptions.HttpClientFactory = () =>
    {
        var handler = new HttpClientHandler
        {
            ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator
        };
        return new HttpClient(handler, true);
    };

builder.Services.AddHangfire(config => config
    .UseSimpleAssemblyNameTypeSerializer()
    .UseRecommendedSerializerSettings()
    .UseAzureCosmosDbStorage(cosmosEndpoint.AbsoluteUri, cosmosAuthSecret, cosmosDatabaseName, hangfireCollectionName,
        cosmosClientOptions));
builder.Services.AddHostedService<HangfireServerHostedService>();

var app = builder.Build();

var logger = app.Services.GetRequiredService<ILogger<Program>>();
logger.LogInformation("Starting Pokémon API service");

// ------------------------------------------------------------
// Global exception middleware must be registered first so it
// can catch exceptions from controllers, MediatR handlers, etc.
// ------------------------------------------------------------
app.UseMiddleware<GlobalExceptionMiddleware>();

// Fallback exception handler for non-validation errors (safety net)
app.UseExceptionHandler(errorApp =>
{
    errorApp.Run(async context =>
    {
        var feature = context.Features.Get<IExceptionHandlerFeature>();
        var ex = feature?.Error;

        context.Response.StatusCode = StatusCodes.Status500InternalServerError;
        context.Response.ContentType = "application/json";

        logger.LogError(ex, "Unhandled exception processing request");

        await context.Response.WriteAsJsonAsync(new { message = "An error occurred while processing your request." });
    });
});

// OpenAPI mapping
app.MapOpenApi();

if (app.Environment.IsDevelopment())
{
    logger.LogInformation("Development environment detected. Enabling Swagger UI and Scalar UI");

    // --- Swagger JSON ---
    app.UseSwagger();

    // --- Swagger UI ---
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/swagger/v1/swagger.json", "Pokémon API v1");
        options.RoutePrefix = "swagger";
        options.DisplayRequestDuration();
        options.DefaultModelsExpandDepth(2);
    });

    // --- Scalar UI ---
    app.MapScalarApiReference(options =>
    {
        options.WithTitle("Pokémon Encyclopedia API");
        options.WithTheme(ScalarTheme.Kepler);
        options.WithDefaultHttpClient(ScalarTarget.CSharp, ScalarClient.HttpClient);
        options.DotNetFlag = true;
    });
}

// Startup logging
app.Lifetime.ApplicationStarted.Register(() =>
{
    logger.LogInformation("🚀 Pokémon API started successfully!");
    foreach (var address in app.Urls) logger.LogInformation("   Base URL: {BaseUrl}", address);
});

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapPrometheusScrapingEndpoint();
app.UseHangfireDashboardUI("/hangfire", new DashboardUIOptions
{
    DashboardTitle = "Pokémon Encyclopedia Jobs",
    DefaultTheme = "auto",
    EnableJobManagement = true,
    Authorization = []
});

logger.LogInformation("Mapping controllers and default endpoints");
app.MapControllers();
if (app.Environment.IsDevelopment())
{
    app.MapHealthChecks("/health", new HealthCheckOptions
    {
        ResultStatusCodes =
        {
            [Microsoft.Extensions.Diagnostics.HealthChecks.HealthStatus.Healthy] = StatusCodes.Status200OK,
            [Microsoft.Extensions.Diagnostics.HealthChecks.HealthStatus.Degraded] = StatusCodes.Status503ServiceUnavailable,
            [Microsoft.Extensions.Diagnostics.HealthChecks.HealthStatus.Unhealthy] = StatusCodes.Status503ServiceUnavailable
        }
    });

    app.MapHealthChecks("/alive", new HealthCheckOptions
    {
        Predicate = r => r.Tags.Contains("live")
    });
}

logger.LogInformation("Pokémon API service started successfully");
app.Run();

static bool IsLocalCosmosEmulatorEndpoint(Uri endpoint)
{
    return endpoint.Scheme == Uri.UriSchemeHttps &&
           (endpoint.Host.Equals("localhost", StringComparison.OrdinalIgnoreCase) ||
            endpoint.Host.Equals("127.0.0.1", StringComparison.OrdinalIgnoreCase) ||
            endpoint.Host.Equals("::1", StringComparison.OrdinalIgnoreCase));
}