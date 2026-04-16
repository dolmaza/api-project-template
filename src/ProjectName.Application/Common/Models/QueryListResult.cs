namespace ProjectName.Application.Common.Models;

public class QueryListResult<T>(IList<T> items)
{
    public IList<T> Items { get; private set; } = items;
}