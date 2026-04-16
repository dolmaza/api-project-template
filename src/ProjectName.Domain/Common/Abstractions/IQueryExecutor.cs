namespace ProjectName.Domain.Common.Abstractions;

public interface IQueryExecutor<in TRequest, TResponse> where TRequest : IQuery
{
    Task<TResponse> ExecuteAsync(TRequest request, CancellationToken cancellationToken = default);
}