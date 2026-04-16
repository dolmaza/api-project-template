using FluentValidation;
using FluentValidation.Results;
using ProjectName.Domain.Common.Mediator.Abstractions;

namespace ProjectName.Infrastructure.Behaviours;

public class ValidatorBehavior<TRequest, TResponse>(IEnumerable<IValidator<TRequest>> validators)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    public async Task<TResponse> Handle(TRequest request, Func<Task<TResponse>> next, CancellationToken cancellationToken)
    {
        var failures = new List<ValidationFailure>();

        foreach (var validator in validators)
        {
            var validationResult = await validator.ValidateAsync(request, cancellationToken);
            var errors = validationResult.Errors.Where(error => error != null).ToList();
            failures.AddRange(errors);
        }

        if (failures.Count != 0)
        {
            throw new ValidationException($"Command Validation Errors for type {typeof(TRequest).Name}", failures);
        }

        var response = await next();

        return response;
    }
}