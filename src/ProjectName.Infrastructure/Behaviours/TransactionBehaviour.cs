using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using ProjectName.Domain.Common.Mediator.Abstractions;
using ProjectName.Infrastructure.Database;

namespace ProjectName.Infrastructure.Behaviours;

public class TransactionBehaviour<TRequest, TResponse>(
    ApplicationDbContext dbContext,
    ILogger<TransactionBehaviour<TRequest, TResponse>> logger)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{

    public async Task<TResponse> Handle(TRequest request, Func<Task<TResponse>> next, CancellationToken cancellationToken)
    {
        TResponse response = default!;

        try
        {
            var strategy = dbContext.Database.CreateExecutionStrategy();
            await strategy.ExecuteAsync(async () =>
            {
                logger.LogInformation("Begin transaction {Name}", typeof(TRequest).Name);

                await dbContext.BeginTransactionAsync(cancellationToken);

                response = await next();

                await dbContext.CommitTransactionAsync(cancellationToken);

                logger.LogInformation("Committed transaction {Name}", typeof(TRequest).Name);

            });

            return response;
        }
        catch (Exception ex)
        {
            logger.LogInformation("Rollback transaction executed {Name}; \n {Exception}", typeof(TRequest).Name, ex.ToString());

            dbContext.RollbackTransaction();
            throw;
        }
    }
}