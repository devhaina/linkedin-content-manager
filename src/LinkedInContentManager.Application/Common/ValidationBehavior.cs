using FluentValidation;
using MediatR;

namespace LinkedInContentManager.Application.Common;

/// <summary>
/// Runs every registered <see cref="IValidator{T}"/> for a request before its handler executes,
/// and throws <see cref="ValidationException"/> on failure. Without this pipeline behavior,
/// the validators registered in <c>DependencyInjection.AddApplicationServices</c> are dead code
/// — MediatR does not run them on its own.
/// </summary>
public sealed class ValidationBehavior<TRequest, TResponse>(IEnumerable<IValidator<TRequest>> validators)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        if (!validators.Any())
            return await next(cancellationToken);

        var context = new ValidationContext<TRequest>(request);
        var failures = (await Task.WhenAll(validators.Select(v => v.ValidateAsync(context, cancellationToken))))
            .SelectMany(result => result.Errors)
            .Where(failure => failure is not null)
            .ToList();

        if (failures.Count > 0)
            throw new ValidationException(failures);

        return await next(cancellationToken);
    }
}
