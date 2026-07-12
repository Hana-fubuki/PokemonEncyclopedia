using Projects;

var builder = DistributedApplication.CreateBuilder(args);
var seq = builder.AddSeq("seq")
    .WithDataVolume()
    .ExcludeFromManifest()
    .WithLifetime(ContainerLifetime.Persistent)
    .WithEnvironment("ACCEPT_EULA", "Y");

var prometheusConfigPath = Path.Combine(builder.AppHostDirectory, "observability", "prometheus");
var grafanaProvisioningPath = Path.Combine(builder.AppHostDirectory, "observability", "grafana", "provisioning");
var prometheusDataPath = Path.Combine(builder.AppHostDirectory, ".data", "prometheus");
var grafanaDataPath = Path.Combine(builder.AppHostDirectory, ".data", "grafana");

var prometheus = builder.AddContainer("prometheus", "prom/prometheus")
    .ExcludeFromManifest()
    .WithHttpEndpoint(targetPort: 9090, port: 9090, name: "http")
    .WithBindMount(prometheusConfigPath, "/etc/prometheus", true)
    .WithBindMount(prometheusDataPath, "/prometheus", false)
    .WithLifetime(ContainerLifetime.Persistent);

var grafana = builder.AddContainer("grafana", "grafana/grafana")
    .ExcludeFromManifest()
    .WithHttpEndpoint(targetPort: 3000, port: 3000, name: "http")
    .WithBindMount(grafanaProvisioningPath, "/etc/grafana/provisioning", true)
    .WithBindMount(grafanaDataPath, "/var/lib/grafana", false)
    .WithLifetime(ContainerLifetime.Persistent)
    .WithEnvironment("GF_SECURITY_ADMIN_USER", "admin")
    .WithEnvironment("GF_SECURITY_ADMIN_PASSWORD", "admin")
    .WithEnvironment("GF_AUTH_ANONYMOUS_ENABLED", "true")
    .WithEnvironment("GF_AUTH_ANONYMOUS_ORG_ROLE", "Admin");

var cache = builder.AddAzureManagedRedis("cache")
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
    .WithHttpEndpoint(targetPort: 5200, port: 9464, name: "metrics", isProxied: true)
    .WithReference(cache)
    .WaitFor(cache)
    .WithReference(hangfireDb)
    .WaitFor(hangfireDb)
    .WithReference(seq)
    .WaitFor(seq)
    .WithEnvironment("ASPNETCORE_ENVIRONMENT", "Development")
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
    .WithHttpEndpoint(targetPort: 5525, port: 9465, name: "metrics", isProxied: true)
    .WithHttpHealthCheck("/health")
    .WithReference(cache)
    .WaitFor(cache)
    .WithReference(seq)
    .WaitFor(seq)
    .WithReference(apiService)
    .WaitFor(apiService);

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