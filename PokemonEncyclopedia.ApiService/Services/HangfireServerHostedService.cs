using Hangfire;
using PokemonEncyclopedia.Infrastructure.Services;

namespace PokemonEncyclopedia.ApiService.Services;

/// <summary>
/// Starts Hangfire server without blocking API startup when storage initialization is slow.
/// </summary>
public sealed class HangfireServerHostedService(
    IServiceProvider serviceProvider,
    ILogger<HangfireServerHostedService> logger) : IHostedService, IDisposable
{
    private BackgroundJobServer? _server;
    private Task? _startupTask;
    private CancellationTokenSource? _cancellationTokenSource;
    private const string RefreshJobId = "pokemon-cache-refresh";
    private const string RefreshJobCron = "0 * * * *";

    public Task StartAsync(CancellationToken cancellationToken)
    {
        _cancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        _startupTask = Task.Run(() => StartServerWithRetryAsync(_cancellationTokenSource.Token), CancellationToken.None);
        return Task.CompletedTask;
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        _cancellationTokenSource?.Cancel();

        if (_startupTask is not null)
            await _startupTask.WaitAsync(cancellationToken).ConfigureAwait(false);

        _server?.Dispose();
        _server = null;
    }

    public void Dispose()
    {
        _server?.Dispose();
        _cancellationTokenSource?.Dispose();
    }

    private async Task StartServerWithRetryAsync(CancellationToken cancellationToken)
    {
        var retryDelays = new[]
        {
            TimeSpan.Zero,
            TimeSpan.FromSeconds(10),
            TimeSpan.FromSeconds(30)
        };

        for (var attempt = 0; attempt < retryDelays.Length; attempt++)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (retryDelays[attempt] > TimeSpan.Zero)
                await Task.Delay(retryDelays[attempt], cancellationToken).ConfigureAwait(false);

            try
            {
                var storage = serviceProvider.GetRequiredService<JobStorage>();
                _server = new BackgroundJobServer(new BackgroundJobServerOptions(), storage);
                logger.LogInformation("Hangfire server started.");
                await RegisterRecurringJobsWithRetryAsync(cancellationToken).ConfigureAwait(false);
                return;
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Hangfire server start attempt {Attempt} failed.", attempt + 1);
            }
        }

        logger.LogError("Failed to start Hangfire server after all retry attempts.");
    }

    private async Task RegisterRecurringJobsWithRetryAsync(CancellationToken cancellationToken)
    {
        var retryDelays = new[]
        {
            TimeSpan.Zero,
            TimeSpan.FromSeconds(10),
            TimeSpan.FromSeconds(30)
        };

        for (var attempt = 0; attempt < retryDelays.Length; attempt++)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (retryDelays[attempt] > TimeSpan.Zero)
                await Task.Delay(retryDelays[attempt], cancellationToken).ConfigureAwait(false);

            try
            {
                var storage = serviceProvider.GetRequiredService<JobStorage>();
                JobStorage.Current = storage;

                RecurringJob.AddOrUpdate<PokemonCacheRefreshJob>(
                    RefreshJobId,
                    job => job.Execute(),
                    RefreshJobCron,
                    new RecurringJobOptions
                    {
                        TimeZone = TimeZoneInfo.Utc
                    });

                logger.LogInformation("Recurring jobs registered.");
                return;
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Recurring job registration attempt {Attempt} failed.", attempt + 1);
            }
        }

        logger.LogError("Failed to register recurring jobs after all retry attempts.");
    }
}
