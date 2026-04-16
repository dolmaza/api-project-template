using Microsoft.AspNetCore.Http;
using ProjectName.Domain.Common.ResultPattern;

namespace ProjectName.Application.Common.Extensions;

public static class ResultExtensions
{
    public static IResult ToProblemDetails(this Result result)
    {
        return result.IsSuccess ? throw new InvalidOperationException() : GetProblemResult(result.Error);
    }

    public static IResult ToProblemDetails<T>(this Result<T> result)
    {
        return result.IsSuccess ? throw new InvalidOperationException() : GetProblemResult(result.Error);
    }

    private static IResult GetProblemResult(Error error) => Results.Problem
    (
        statusCode: GetStatusCode(error.Type),
        title: GetTitle(error.Type),
        type: GetType(error.Type),
        extensions: new Dictionary<string, object?>
        {
            { "error", error }
        }
    );

    private static int GetStatusCode(ErrorType errorType) => errorType switch
    {
        ErrorType.Validation or ErrorType.Failure => StatusCodes.Status400BadRequest,
        ErrorType.NotFound => StatusCodes.Status404NotFound,
        ErrorType.Conflict => StatusCodes.Status409Conflict,
        _ => StatusCodes.Status500InternalServerError
    };

    private static string GetTitle(ErrorType errorType) => errorType switch
    {
        ErrorType.Validation or ErrorType.Failure => "Bad Request",
        ErrorType.NotFound => "Not Found",
        ErrorType.Conflict => "Conflict",
        _ => "Server Failure"
    };

    private static string GetType(ErrorType errorType) => errorType switch
    {
        ErrorType.Validation or ErrorType.Failure => "https://tools.ietf.org/html/rfc7231#section-6.5.1",
        ErrorType.NotFound => "https://tools.ietf.org/html/rfc7231#section-6.5.4",
        ErrorType.Conflict => "https://tools.ietf.org/html/rfc7231#section-6.5.8",
        _ => "https://tools.ietf.org/html/rfc7231#section-6.6.1"
    };
}