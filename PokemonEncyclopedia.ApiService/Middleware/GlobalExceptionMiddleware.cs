using System.Linq;
using System.Threading.Tasks;
using FluentValidation;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace PokemonEncyclopedia.ApiService.Middleware
{
    /// <summary>
    ///     Captures unhandled exceptions and returns consistent JSON error responses.
    /// </summary>
    public sealed class GlobalExceptionMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<GlobalExceptionMiddleware> _logger;

        /// <summary>
        ///     Initializes a new instance of the <see cref="GlobalExceptionMiddleware" /> class.
        /// </summary>
        /// <param name="next">The next middleware in the pipeline.</param>
        /// <param name="logger">Logger for exception and pipeline diagnostics.</param>
        public GlobalExceptionMiddleware(RequestDelegate next, ILogger<GlobalExceptionMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        /// <summary>
        ///     Executes middleware logic and maps known exceptions to HTTP responses.
        /// </summary>
        /// <param name="context">The current HTTP request context.</param>
        /// <returns>A task that completes when request processing is done.</returns>
        public async Task InvokeAsync(HttpContext context)
        {
            // top of InvokeAsync
            _logger.LogDebug("GlobalExceptionMiddleware: entering for {Method} {Path}", context.Request.Method, context.Request.Path);

            try
            {
                await _next(context);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unhandled exception while processing request {Method} {Path}", context.Request.Method, context.Request.Path);

                if (context.Response.HasStarted)
                {
                    _logger.LogWarning("Response already started for {Path}; cannot write error payload", context.Request.Path);
                    throw;
                }

                context.Response.Clear();

                if (ex is ValidationException fvEx)
                {
                    _logger.LogWarning(fvEx, "GlobalExceptionMiddleware: mapping ValidationException to 400");
                    context.Response.StatusCode = StatusCodes.Status400BadRequest;
                    context.Response.ContentType = "application/json";

                    var errors = fvEx.Errors
                        .GroupBy(e => e.PropertyName)
                        .Select(g => new { Field = g.Key, Errors = g.Select(e => e.ErrorMessage) });

                    var payload = new { message = "Validation failed", errors };
                    await context.Response.WriteAsJsonAsync(payload);
                    return;
                }

                context.Response.StatusCode = StatusCodes.Status500InternalServerError;
                context.Response.ContentType = "application/json";
                await context.Response.WriteAsJsonAsync(new { message = "An error occurred while processing your request." });
            }
        }
    }
}
