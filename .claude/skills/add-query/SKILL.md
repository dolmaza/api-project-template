---
name: add-query
description: Scaffold the full read/query stack for an existing aggregate — query record, query executor (Dapper + SqlBuilder), query reader interface and implementation, and DTO classes. Use when the user asks to add a list, paged, or by-id query for an aggregate.
allowed-tools: Read Write Glob Grep
---

You are an expert in this codebase's query-side patterns. Given an aggregate name and the fields to expose, generate all files needed to support a Dapper-based read query bypassing EF Core.

## User's Request

$ARGUMENTS

---

## What to Generate

Under `src/ProjectName.Application/{Domain}/Queries/`:

1. `Dtos/{AggregateName}ListDto.cs` — flat DTO for list/paged views
2. `Dtos/{AggregateName}Dto.cs` — richer DTO for detail/by-id views
3. `Get{AggregateName}s/Get{AggregateName}sQuery.cs` — paged list query record
4. `Get{AggregateName}s/Get{AggregateName}sQueryExecutor.cs` — executor using SqlBuilder
5. `Get{AggregateName}ById/Get{AggregateName}ByIdQuery.cs` — single-item query record
6. `Get{AggregateName}ById/Get{AggregateName}ByIdQueryExecutor.cs` — executor using raw SQL
7. `{AggregateName}QueryReader.cs` — Dapper I/O wrapper (interface + implementation in same file)

If `{Domain}Errors.cs` does not yet exist at `src/ProjectName.Application/{Domain}/{Domain}Errors.cs`, create it with the `{AggregateName}NotFound` error.

---

## File Templates

### 1. DTOs

```csharp
// Dtos/{AggregateName}ListDto.cs
namespace ProjectName.Application.{Domain}.Queries.Dtos;

public record {AggregateName}ListDto({KeyType} Id, string Name /*, subset of fields needed for list */);
```

```csharp
// Dtos/{AggregateName}Dto.cs
using ProjectName.Domain.AggregatesModel.{Domain}Aggregate;

namespace ProjectName.Application.{Domain}.Queries.Dtos;

public record {AggregateName}Dto({KeyType} Id, string Name /*, all fields needed for detail view */);
```

---

### 2. Paged List Query

```csharp
// Get{AggregateName}s/Get{AggregateName}sQuery.cs
using ProjectName.Domain.Common.Abstractions;

namespace ProjectName.Application.{Domain}.Queries.Get{AggregateName}s;

public record Get{AggregateName}sQuery(string? SearchValue, int Skip, int Take) : IQuery;
```

```csharp
// Get{AggregateName}s/Get{AggregateName}sQueryExecutor.cs
using Dapper;
using Microsoft.Extensions.Options;
using ProjectName.Application.Common.Configs;
using ProjectName.Application.Common.Extensions;
using ProjectName.Application.Common.Models;
using ProjectName.Application.{Domain}.Queries;
using ProjectName.Application.{Domain}.Queries.Dtos;
using ProjectName.Domain.AggregatesModel.{Domain}Aggregate;
using ProjectName.Domain.Common.Abstractions;
using ProjectName.Domain.Common.ResultPattern;

namespace ProjectName.Application.{Domain}.Queries.Get{AggregateName}s;

public class Get{AggregateName}sQueryExecutor(
    IOptions<ConnectionStringsConfig> config,
    I{AggregateName}QueryReader? queryReader = null)
    : IQueryExecutor<Get{AggregateName}sQuery, Result<QueryPagedListResult<{AggregateName}ListDto>>>
{
    private readonly string _connectionString = config.Value.DefaultConnection;
    private readonly I{AggregateName}QueryReader _queryReader = queryReader ?? new {AggregateName}QueryReader();
    private const string Alias = "e";  // short alias for the table

    public async Task<Result<QueryPagedListResult<{AggregateName}ListDto>>> ExecuteAsync(
        Get{AggregateName}sQuery request, CancellationToken cancellationToken = default)
    {
        var builder = new SqlBuilder();
        var parameters = new DynamicParameters();

        var template = builder.AddTemplate(
            $"""
             SELECT {Alias}."{nameof({AggregateName}.Id)}",
                    {Alias}."{nameof({AggregateName}.Name)}"
             FROM application."{AggregateName}s" {Alias}
             /**where**/
             ORDER BY {Alias}."CreatedAt" DESC
             OFFSET @skip LIMIT @take;

             SELECT COUNT({Alias}."Id")
             FROM application."{AggregateName}s" {Alias}
             /**where**/
             """);

        parameters.Add("skip", request.Skip);
        parameters.Add("take", request.Take);

        ApplyFilters(builder, parameters, request.SearchValue);

        var result = await _queryReader.Get{AggregateName}sAsync(
            _connectionString, template.RawSql, parameters, cancellationToken);

        return new QueryPagedListResult<{AggregateName}ListDto>(result.TotalCount, result.Items.ToList());
    }

    private static void ApplyFilters(SqlBuilder builder, DynamicParameters parameters, string? searchValue)
    {
        builder
            .ApplySoftDeleteFilter(Alias)
            .ApplySearchFilter(
                [new DatabaseColumn(nameof({AggregateName}.Name), Alias)],
                parameters,
                searchValue);
    }
}
```

---

### 3. By-Id Query

```csharp
// Get{AggregateName}ById/Get{AggregateName}ByIdQuery.cs
using ProjectName.Domain.Common.Abstractions;

namespace ProjectName.Application.{Domain}.Queries.Get{AggregateName}ById;

public record Get{AggregateName}ByIdQuery({KeyType} Id) : IQuery;
```

```csharp
// Get{AggregateName}ById/Get{AggregateName}ByIdQueryExecutor.cs
using Microsoft.Extensions.Options;
using ProjectName.Application.Common.Configs;
using ProjectName.Application.{Domain}.Queries;
using ProjectName.Application.{Domain}.Queries.Dtos;
using ProjectName.Domain.AggregatesModel.{Domain}Aggregate;
using ProjectName.Domain.Common.Abstractions;
using ProjectName.Domain.Common.ResultPattern;

namespace ProjectName.Application.{Domain}.Queries.Get{AggregateName}ById;

public class Get{AggregateName}ByIdQueryExecutor(
    IOptions<ConnectionStringsConfig> config,
    I{AggregateName}QueryReader? queryReader = null)
    : IQueryExecutor<Get{AggregateName}ByIdQuery, Result<{AggregateName}Dto>>
{
    private readonly string _connectionString = config.Value.DefaultConnection;
    private readonly I{AggregateName}QueryReader _queryReader = queryReader ?? new {AggregateName}QueryReader();
    private const string Alias = "e";

    public async Task<Result<{AggregateName}Dto>> ExecuteAsync(
        Get{AggregateName}ByIdQuery request, CancellationToken cancellationToken = default)
    {
        var item = await _queryReader.Get{AggregateName}ByIdAsync(
            _connectionString,
            $"""
             SELECT {Alias}."{nameof({AggregateName}.Id)}",
                    {Alias}."{nameof({AggregateName}.Name)}"
             FROM application."{AggregateName}s" {Alias}
             WHERE {Alias}."{nameof({AggregateName}.Id)}" = @id
               AND {Alias}."DeletedAt" IS NULL
             """,
            new { id = request.Id },
            cancellationToken);

        if (item is null)
            return Result.Failure({Domain}Errors.{AggregateName}NotFound);

        return item;
    }
}
```

---

### 4. QueryReader (interface + implementation in one file)

```csharp
// {AggregateName}QueryReader.cs
using Dapper;
using Npgsql;
using ProjectName.Application.{Domain}.Queries.Dtos;
using ProjectName.Application.Common.Models;

namespace ProjectName.Application.{Domain}.Queries;

public interface I{AggregateName}QueryReader
{
    Task<(IReadOnlyList<{AggregateName}ListDto> Items, int TotalCount)> Get{AggregateName}sAsync(
        string connectionString,
        string sql,
        DynamicParameters parameters,
        CancellationToken cancellationToken = default);

    Task<{AggregateName}Dto?> Get{AggregateName}ByIdAsync(
        string connectionString,
        string sql,
        object parameters,
        CancellationToken cancellationToken = default);
}

public class {AggregateName}QueryReader : I{AggregateName}QueryReader
{
    public async Task<(IReadOnlyList<{AggregateName}ListDto> Items, int TotalCount)> Get{AggregateName}sAsync(
        string connectionString,
        string sql,
        DynamicParameters parameters,
        CancellationToken cancellationToken = default)
    {
        await using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync(cancellationToken);

        var query = await connection.QueryMultipleAsync(
            new CommandDefinition(sql, parameters, cancellationToken: cancellationToken));

        var items = (await query.ReadAsync<{AggregateName}ListDto>()).ToList();
        var totalCount = await query.ReadFirstOrDefaultAsync<int>();

        return (items, totalCount);
    }

    public async Task<{AggregateName}Dto?> Get{AggregateName}ByIdAsync(
        string connectionString,
        string sql,
        object parameters,
        CancellationToken cancellationToken = default)
    {
        await using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync(cancellationToken);

        return await connection.QuerySingleOrDefaultAsync<{AggregateName}Dto>(
            new CommandDefinition(sql, parameters, cancellationToken: cancellationToken));
    }
}
```

---

## SqlBuilder Filter Extensions Available

The `SqlBuilder` extensions defined in `Application/Common/Extensions/SqlBuilderFilterExtensions.cs` are:

```csharp
builder.ApplySoftDeleteFilter(alias)                          // adds "alias"."DeletedAt" IS NULL
builder.ApplySearchFilter(columns[], parameters, searchValue) // adds ILIKE filter on given columns
builder.ApplyUserFilter(currentStateService, parameters, col) // adds userId ownership filter (admin bypasses)
builder.ApplyCustomFilter(value, parameters, col)             // adds exact-match filter for any field
```

Always call `ApplySoftDeleteFilter` for queries on soft-deletable tables.

The `DatabaseColumn` helper:
```csharp
new DatabaseColumn("ColumnName", "alias")  // produces alias."ColumnName"
```

---

## Important Rules

- **Never** use EF Core `DbContext` or `DbSet` in query executors — Dapper + raw SQL only
- Always include both the paged data query and the `COUNT(...)` query in one round-trip via `QueryMultipleAsync`
- By-id executors use `QuerySingleOrDefaultAsync` (not `QueryMultipleAsync`)
- Always filter `DeletedAt IS NULL` manually in the by-id SQL (no `ApplySoftDeleteFilter` needed for single-statement queries)
- Query executors are registered by concrete type (not interface); they must be discoverable by `AddAllQueryExecutors`
- The `I{AggregateName}QueryReader` default parameter `= null` + `?? new {AggregateName}QueryReader()` makes executors unit-testable without DI

## Namespace and File Placement

- Namespace root: `ProjectName.Application.{Domain}.Queries`
- DTOs namespace: `ProjectName.Application.{Domain}.Queries.Dtos`
- Executor namespaces: `ProjectName.Application.{Domain}.Queries.Get{AggregateName}s` etc.

## Checklist Before Finishing

- [ ] `{AggregateName}ListDto` is a `record` with only the fields needed for list views
- [ ] `{AggregateName}Dto` is a `record` with all fields needed for detail views
- [ ] List executor uses `SqlBuilder` with `/**where**/` placeholder
- [ ] List executor calls `ApplySoftDeleteFilter` before executing
- [ ] List executor fetches data + count in one `QueryMultipleAsync` call
- [ ] By-id executor uses raw SQL string with `DeletedAt IS NULL` inline
- [ ] By-id executor returns `{Domain}Errors.{AggregateName}NotFound` when `null`
- [ ] `I{AggregateName}QueryReader` interface and `{AggregateName}QueryReader` class are in the same file
- [ ] QueryReader constructor parameter `queryReader = null` allows injection or default instantiation
- [ ] `{Domain}Errors.cs` exists
