using Microsoft.Extensions.Logging;
using ProjectName.Domain.Common.Mediator.Abstractions;

namespace ProjectName.Infrastructure.Behaviours;

public class LoggingBehavior<TRequest, TResponse>(ILogger<LoggingBehavior<TRequest, TResponse>> logger) : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    public async Task<TResponse> Handle(TRequest request, Func<Task<TResponse>> next, CancellationToken cancellationToken)
    {
        var requestName = typeof(TRequest).Name;

        logger.LogInformation("Handling {RequestName}", requestName);
        var response = await next();
        logger.LogInformation("Handled {RequestName}", requestName);

        return response;
    }
}