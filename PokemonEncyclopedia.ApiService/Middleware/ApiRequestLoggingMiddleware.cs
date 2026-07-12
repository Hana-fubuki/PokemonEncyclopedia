using System.Diagnostics;
using Microsoft.Extensions.Logging;
using PokemonEncyclopedia.ServiceDefaults;

namespace PokemonEncyclopedia.ApiService.Middleware;

public sealed class ApiRequestLoggingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ApiRequestLoggingMiddleware> _logger;

    public ApiRequestLoggingMiddleware(RequestDelegate next, ILogger<ApiRequestLoggingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var sw = Stopwatch.StartNew();
        try
        {
            await _next(context);
        }
        finally
        {
            sw.Stop();

            var endpoint = context.GetEndpoint()?.DisplayName ?? context.Request.Path.Value ?? "unknown";
            Telemetry.ApiRequests.Add(1,
                new KeyValuePair<string, object?>("http.method", context.Request.Method),
                new KeyValuePair<string, object?>("http.route", endpoint),
                new KeyValuePair<string, object?>("http.status_code", context.Response.StatusCode));
            Telemetry.ApiRequestDuration.Record(sw.Elapsed.TotalMilliseconds,
                new KeyValuePair<string, object?>("http.method", context.Request.Method),
                new KeyValuePair<string, object?>("http.route", endpoint),
                new KeyValuePair<string, object?>("http.status_code", context.Response.StatusCode));
            _logger.LogInformation(
                "HTTP {Method} {Path} responded {StatusCode} in {ElapsedMs}ms ({Endpoint})",
                context.Request.Method,
                context.Request.Path,
                context.Response.StatusCode,
                sw.ElapsedMilliseconds,
                endpoint);
        }
    }
}
