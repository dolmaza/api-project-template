using ProjectName.Application.Common.Abstractions;
using ProjectName.Domain.Common.ResultPattern;

namespace ProjectName.API.Infrastructure.Filters;

public class IdempotencyCheckerFilter(IRequestManager requestManager) : IEndpointFilter
{
    private const string RequestIdHeader = "x-request-id";
    private readonly string[] _allowedHttpMethods = [HttpMethods.Post, HttpMethods.Put, HttpMethods.Delete];

    public async ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
    {
        if (!_allowedHttpMethods.Contains(context.HttpContext.Request.Method))
        {
            return await next(context);
        }

        var request = context.HttpContext.Request;
        Guid requestId;

        if (!request.Headers.TryGetValue(RequestIdHeader, out var requestIdValues) ||
                   string.IsNullOrWhiteSpace(requestIdValues.FirstOrDefault()))
        {
            return Results.BadRequest(IdentifiedErrors.MissingOrEmptyRequestId(RequestIdHeader));
        }
        else if (!Guid.TryParse(requestIdValues.First(), out requestId))
        {
            return Results.BadRequest(IdentifiedErrors.InvalidRequestId(RequestIdHeader));
        }
        else if (await requestManager.ExistAsync(requestId))
        {
            return Results.BadRequest(IdentifiedErrors.DuplicateRequest);
        }

        await requestManager.CreateOrUpdateClientRequestAsync(requestId, request.Path.ToString());

        return await next(context);
    }
}

public static class IdentifiedErrors
{
    public static Error MissingOrEmptyRequestId(string requestIdHeaderName) => Error.Failure("MissingOrEmpty", $"Missing or empty '{requestIdHeaderName}' header.");

    public static Error InvalidRequestId(string requestIdHeaderName) => Error.Failure("InvalidRequest", $"Invalid '{requestIdHeaderName}' header format. Must be a GUID.");

    public static readonly Error DuplicateRequest = Error.Failure("DuplicateRequest", "Duplicate request detected.");
}