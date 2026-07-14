using FluentValidation;
using Microsoft.AspNetCore.Mvc;

namespace PokemonEncyclopedia.ApiService.Middleware;

public sealed class ApiExceptionMiddleware
{
    private readonly ILogger<ApiExceptionMiddleware> _logger;
    private readonly RequestDelegate _next;

    public ApiExceptionMiddleware(RequestDelegate next, ILogger<ApiExceptionMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (ValidationException ex)
        {
            await WriteValidationProblemAsync(context, ex);
        }
        catch (Exception ex)
        {
            await WriteServerErrorAsync(context, ex);
        }
    }

    private async Task WriteValidationProblemAsync(HttpContext context, ValidationException exception)
    {
        _logger.LogWarning(exception, "Validation failed while processing request");

        if (context.Response.HasStarted)
        {
            _logger.LogWarning("Response already started; cannot write validation payload");
            throw exception;
        }

        context.Response.Clear();
        context.Response.StatusCode = StatusCodes.Status400BadRequest;
        context.Response.ContentType = "application/problem+json";

        var errors = exception.Errors
            .GroupBy(e => e.PropertyName)
            .ToDictionary(
                g => g.Key,
                g => g.Select(e => e.ErrorMessage).ToArray());

        var payload = new ValidationProblemDetails(errors)
        {
            Title = "Validation failed",
            Status = StatusCodes.Status400BadRequest,
            Detail = "One or more validation rules failed."
        };

        await context.Response.WriteAsJsonAsync(payload);
    }

    private async Task WriteServerErrorAsync(HttpContext context, Exception exception)
    {
        _logger.LogError(exception, "Unhandled exception while processing request");

        if (context.Response.HasStarted)
        {
            _logger.LogWarning("Response already started; cannot write error payload");
            throw exception;
        }

        context.Response.Clear();
        context.Response.StatusCode = StatusCodes.Status500InternalServerError;
        context.Response.ContentType = "application/problem+json";

        await context.Response.WriteAsJsonAsync(new ProblemDetails
        {
            Title = "An unexpected error occurred.",
            Status = StatusCodes.Status500InternalServerError,
            Detail = "An error occurred while processing your request."
        });
    }
}