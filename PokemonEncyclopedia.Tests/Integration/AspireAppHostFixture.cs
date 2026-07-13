using System.Collections;

namespace PokemonEncyclopedia.Tests.Integration;

public sealed class AspireAppHostFixture : IAsyncLifetime
{
    private const string ApiResourceName = "apiservice";
    private const string WebResourceName = "webfrontend";

    private Func<HttpClient>? _createApiClient;
    private Func<HttpClient>? _createWebClient;
    private Func<string, CancellationToken, Task>? _waitForHealthy;
    private Func<string, string>? _describeResourceState;
    private Func<Task>? _disposeApp;
    public async Task InitializeAsync()
    {
        using var startupCts = new CancellationTokenSource(TestExecutionSettings.IntegrationStartupTimeout);

        try
        {
            var appHost =
                await DistributedApplicationTestingBuilder.CreateAsync<Projects.PokemonEncyclopedia_AppHost>(
                    ["--INTEGRATION_TEST_MODE=true"],
                    startupCts.Token);
            appHost.Services.ConfigureHttpClientDefaults(clientBuilder =>
            {
                clientBuilder.AddStandardResilienceHandler();
            });

            var app = await appHost.BuildAsync(startupCts.Token)
                .WaitAsync(TestExecutionSettings.IntegrationStartupTimeout, startupCts.Token);
            await app.StartAsync(startupCts.Token).WaitAsync(TestExecutionSettings.IntegrationStartupTimeout, startupCts.Token);

            _createApiClient = () => app.CreateHttpClient(ApiResourceName);
            _createWebClient = () => app.CreateHttpClient(WebResourceName);
            _waitForHealthy = (resourceName, token) => app.ResourceNotifications
                .WaitForResourceHealthyAsync(resourceName, token)
                .WaitAsync(TestExecutionSettings.IntegrationStartupTimeout, token);
            _describeResourceState = resourceName =>
            {
                if (!app.ResourceNotifications.TryGetCurrentState(resourceName, out var resourceEvent))
                    return $"{resourceName}: state-unavailable";

                var snapshot = resourceEvent.Snapshot;
                var snapshotType = snapshot.GetType();
                var state = snapshotType.GetProperty("State")?.GetValue(snapshot)?.ToString() ?? "unknown";
                var urls = snapshotType.GetProperty("Urls")?.GetValue(snapshot) as IEnumerable;
                var urlList = new List<string>();

                if (urls is not null)
                {
                    foreach (var url in urls)
                    {
                        if (url is null) continue;
                        var urlValue = url.GetType().GetProperty("Url")?.GetValue(url)?.ToString();
                        if (!string.IsNullOrWhiteSpace(urlValue))
                            urlList.Add(urlValue);
                    }
                }

                var urlsText = urlList.Count > 0 ? string.Join(", ", urlList) : "none";
                return $"{resourceName}: state={state}, urls=[{urlsText}]";
            };
            _disposeApp = async () =>
            {
                await app.DisposeAsync();
            };

            await EnsureApiHealthyAsync(startupCts.Token);
            await EnsureWebHealthyAsync(startupCts.Token);
        }
        catch (Exception ex) when (ex is TimeoutException or OperationCanceledException or TaskCanceledException)
        {
            throw new TimeoutException(
                $"Aspire AppHost integration startup timed out after {TestExecutionSettings.IntegrationStartupTimeout}. {BuildStartupDiagnostics()}",
                ex);
        }
    }

    public async Task DisposeAsync()
    {
        if (_disposeApp is not null)
        {
            await _disposeApp();
            _createApiClient = null;
            _createWebClient = null;
            _waitForHealthy = null;
            _describeResourceState = null;
            _disposeApp = null;
        }
    }

    public HttpClient CreateApiClient() =>
        _createApiClient is not null
            ? _createApiClient()
            : throw new InvalidOperationException("Aspire test app has not been initialized.");

    public HttpClient CreateWebClient() =>
        _createWebClient is not null
            ? _createWebClient()
            : throw new InvalidOperationException("Aspire test app has not been initialized.");

    public Task EnsureApiHealthyAsync(CancellationToken cancellationToken = default) =>
        EnsureResourceHealthyAsync(ApiResourceName, cancellationToken);

    public Task EnsureWebHealthyAsync(CancellationToken cancellationToken = default) =>
        EnsureResourceHealthyAsync(WebResourceName, cancellationToken);

    private async Task EnsureResourceHealthyAsync(string resourceName, CancellationToken cancellationToken = default)
    {
        if (_waitForHealthy is null)
            throw new InvalidOperationException("Aspire test app has not been initialized.");

        try
        {
            await _waitForHealthy(resourceName, cancellationToken);
        }
        catch (Exception ex) when (ex is TimeoutException or OperationCanceledException or TaskCanceledException)
        {
            throw new TimeoutException(
                $"Timed out waiting for resource '{resourceName}' to become healthy. {BuildStartupDiagnostics()}",
                ex);
        }
    }

    private string BuildStartupDiagnostics()
    {
        var apiState = DescribeResourceState(ApiResourceName);
        var webState = DescribeResourceState(WebResourceName);
        return $"Resource diagnostics => {apiState}; {webState}";
    }

    private string DescribeResourceState(string resourceName)
    {
        if (_describeResourceState is null)
            return $"{resourceName}: app-not-initialized";
        return _describeResourceState(resourceName);
    }
}
