using Projects;

var builder = DistributedApplication.CreateBuilder(args);

// Determine deployment mode
var deploymentMode = builder.Configuration["DEPLOYMENT_MODE"] ?? "local";
var isAzureDeployment = deploymentMode.Equals("azure", StringComparison.OrdinalIgnoreCase);

// Configure Redis - use Azure or local
var cache = isAzureDeployment
    ? builder.AddAzureManagedRedis("cache")
    : builder.AddAzureManagedRedis("cache")
        .RunAsContainer(redis => redis
            .WithDataVolume()
            .WithLifetime(ContainerLifetime.Persistent));

var cosmos = builder.AddAzureCosmosDB("cosmos")
    .RunAsEmulator(emulator => emulator
        .WithDataVolume()
        .WithLifetime(ContainerLifetime.Persistent));
var hangfireDb = cosmos.AddCosmosDatabase("hangfiredb");

var apiService = builder.AddProject<PokemonEncyclopedia_ApiService>("apiservice")
    .WithHttpHealthCheck("/health")
    .WithExternalHttpEndpoints()
    .WithReference(cache)
    .WaitFor(cache)
    .WithReference(hangfireDb)
    .WaitFor(hangfireDb)
    .WithEnvironment("ASPNETCORE_ENVIRONMENT", isAzureDeployment ? "Production" : "Development")
    .WithEnvironment("DEPLOYMENT_MODE", deploymentMode)
    .WithCommand(
        "open-swagger",
        "Open Swagger",
        context => BuildOpenPathCommandResult(context, "/swagger"),
        new CommandOptions
        {
            Description = "Shows the Swagger URL for the API service.",
            UpdateState = context => GetPublicBaseUrl(context.ResourceSnapshot) is null
                ? ResourceCommandState.Disabled
                : ResourceCommandState.Enabled
        })
    .WithCommand(
        "open-scalar",
        "Open Scalar",
        context => BuildOpenPathCommandResult(context, "/scalar"),
        new CommandOptions
        {
            Description = "Shows the Scalar URL for the API service.",
            UpdateState = context => GetPublicBaseUrl(context.ResourceSnapshot) is null
                ? ResourceCommandState.Disabled
                : ResourceCommandState.Enabled
        })
    .WithCommand(
        "open-hangfire",
        "Open Hangfire",
        context => BuildOpenPathCommandResult(context, "/hangfire"),
        new CommandOptions
        {
            Description = "Shows the Hangfire dashboard URL for the API service.",
            UpdateState = context => GetPublicBaseUrl(context.ResourceSnapshot) is null
                ? ResourceCommandState.Disabled
                : ResourceCommandState.Enabled
        });

builder.AddProject<PokemonEncyclopedia_Web>("webfrontend")
    .WithExternalHttpEndpoints()
    .WithHttpHealthCheck("/health")
    .WithReference(cache)
    .WaitFor(cache)
    .WithReference(apiService)
    .WaitFor(apiService)
    .WithEnvironment("DEPLOYMENT_MODE", deploymentMode);

builder.Build().Run();

static Task<ExecuteCommandResult> BuildOpenPathCommandResult(ExecuteCommandContext context, string path)
{
    var baseUrl =
        context.ServiceProvider.GetService(typeof(ResourceNotificationService)) is ResourceNotificationService
            notifications &&
        notifications.TryGetCurrentState(context.ResourceName, out var resourceEvent)
            ? GetPublicBaseUrl(resourceEvent.Snapshot)
            : null;

    if (string.IsNullOrWhiteSpace(baseUrl))
        return Task.FromResult(new ExecuteCommandResult
        {
            Success = false,
            Message = "No public API URL is available yet. Wait for the resource to start."
        });

    var targetUrl = $"{baseUrl.TrimEnd('/')}{path}";
    return Task.FromResult(new ExecuteCommandResult
    {
        Success = true,
        Message = targetUrl,
        Data = new CommandResultData
        {
            DisplayImmediately = true,
            Format = CommandResultFormat.Markdown,
            Value = $"[{targetUrl}]({targetUrl})"
        }
    });
}

static string? GetPublicBaseUrl(CustomResourceSnapshot snapshot)
{
    return snapshot.Urls
        .FirstOrDefault(url => url is { IsInactive: false, IsInternal: false } &&
                               (url.Url.StartsWith("http://", StringComparison.OrdinalIgnoreCase) ||
                                url.Url.StartsWith("https://", StringComparison.OrdinalIgnoreCase)))
        ?.Url;
}