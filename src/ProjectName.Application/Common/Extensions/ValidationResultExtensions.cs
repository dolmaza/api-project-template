using FluentValidation.Results;
using ProjectName.Domain.Common.ResultPattern;

namespace ProjectName.Application.Common.Extensions;

public static class ValidationResultExtensions
{
    public static Result ToResult(this ValidationResult validationResult)
    {
        return validationResult.IsValid ? Result.Success() : validationResult.Errors.ToResult();
    }

    private static Result ToResult(this IEnumerable<ValidationFailure> validationFailures)
    {
        var validationItems = validationFailures
            .GroupBy(e => e.PropertyName)
            .Select(v => new ValidationItem
            (
                v.Key,
                v.Select(e => e.ErrorMessage
                ).ToArray()))
            .ToList();

        return validationItems.Count == 0
            ? Result.Success() 
            : Result.Failure(Error.Validation("ValidationErrors", "Validation errors occured while validating request", validationItems));
    }
}