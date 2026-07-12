using FluentValidation;
using MediatR;

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

    /// <summary>
    ///     Initializes a new instance of the <see cref="ValidationBehavior{TRequest,TResponse}" /> class.
    /// </summary>
    /// <param name="validators">Validators registered for the request type.</param>
    /// <param name="logger">Logger used for validation diagnostics.</param>
    public ValidationBehavior(IEnumerable<IValidator<TRequest>> validators,
        ILogger<ValidationBehavior<TRequest, TResponse>> logger)
    {
        _validators = validators;
        _logger = logger;
    }

    /// <summary>
    ///     Validates the request before passing execution to the next pipeline delegate.
    /// </summary>
    /// <param name="request">Incoming request.</param>
    /// <param name="next">Next delegate in the MediatR pipeline.</param>
    /// <param name="cancellationToken">Token used to cancel validation.</param>
    /// <returns>The response from the next delegate when validation succeeds.</returns>
    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        if (_validators.Any())
        {
            var context = new ValidationContext<TRequest>(request);
            var validationResults =
                await Task.WhenAll(_validators.Select(v => v.ValidateAsync(context, cancellationToken)));
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