using FluentValidation;
using MediatR;
using Microsoft.Extensions.Logging;

namespace PokemonEncyclopedia.Application.Validators.Behavior;

/// <summary>
///     MediatR pipeline behavior that validates incoming requests using FluentValidation.
/// </summary>
/// <typeparam name="TRequest">Request type being validated.</typeparam>
/// <typeparam name="TResponse">Response type returned by the next handler.</typeparam>
public class ValidationBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull, IRequest<TResponse>
{
    private readonly ILogger<ValidationBehavior<TRequest, TResponse>> _logger;
    private readonly IEnumerable<IValidator<TRequest>> _validators;

    public ValidationBehavior(IEnumerable<IValidator<TRequest>> validators,
        ILogger<ValidationBehavior<TRequest, TResponse>> logger)
    {
        _validators = validators;
        _logger = logger;
    }

    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        if (_validators.Any())
        {
            var context = new ValidationContext<TRequest>(request);
            var validationResults =
                await Task.WhenAll(_validators.Select(v => v.ValidateAsync(context, CancellationToken.None)));
            var failures = validationResults.SelectMany(r => r.Errors).Where(f => f != null).ToList();

            if (failures.Count != 0)
            {
                _logger.LogWarning("ValidationBehavior: {Count} failures for {RequestType}", failures.Count,
                    typeof(TRequest).Name);

                foreach (var g in failures.GroupBy(f => f.PropertyName))
                    _logger.LogDebug("Validation errors for {Property}: {Errors}", g.Key,
                        string.Join("; ", g.Select(e => e.ErrorMessage)));

                throw new ValidationException(failures);
            }
        }

        return await next();
    }
}
