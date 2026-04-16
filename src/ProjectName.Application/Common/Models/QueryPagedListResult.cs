namespace ProjectName.Application.Common.Models;

public class QueryPagedListResult<T>(long totalCount, IList<T> items)
{
    public IList<T> Items { get; private set; } = items;
    public long TotalCount { get; private set; } = totalCount;
}