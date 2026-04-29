---
name: add-repository
description: Scaffold a concrete repository class for an existing aggregate, wire it to the generic base Repository<TEntity, TKey>, and register it in DependencyInjection.cs. Use when the user asks to add the infrastructure repository implementation for a domain aggregate.
allowed-tools: Read Write Glob Grep
---

You are an expert in this codebase's repository and infrastructure patterns. Given an aggregate name, generate the concrete repository class and register it in the DI container.

## User's Request

$ARGUMENTS

---

## What to Generate

1. `src/ProjectName.Infrastructure/Repositories/{AggregateName}Repository.cs`
2. Update `src/ProjectName.Infrastructure/DependencyInjection.cs` — add `AddScoped` registration

---

## Repository Class Template

```csharp
using Microsoft.EntityFrameworkCore;
using ProjectName.Domain.AggregatesModel.{Domain}Aggregate;
using ProjectName.Infrastructure.Database;

namespace ProjectName.Infrastructure.Repositories;

public class {AggregateName}Repository(ApplicationDbContext context)
    : Repository<{AggregateName}, {KeyType}>(context), I{AggregateName}Repository
{
    // Only add methods that extend the generic base
    // Examples:

    public async Task<{AggregateName}?> FindByNameAsync(string name, CancellationToken cancellationToken = default)
    {
        return await DbItems
            .Where(x => x.Name == name && x.DeletedAt == null)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<{AggregateName}>> FindByIdsAsync(
        IEnumerable<{KeyType}> ids, CancellationToken cancellationToken = default)
    {
        var idList = ids.ToList();
        if (idList.Count == 0) return [];
        return await DbItems
            .Where(x => idList.Contains(x.Id) && x.DeletedAt == null)
            .ToListAsync(cancellationToken);
    }
}
```

---

## Generic Base Class Reference

The `Repository<TEntity, TKey>` base already provides:

| Method | Behavior |
|---|---|
| `FindByIdAsync(TKey id)` | Finds by PK; returns `null` if soft-deleted |
| `AddAsync(entity, ct)` | `context.Set<T>().AddAsync(...)` |
| `Update(entity)` | `context.Set<T>().Update(...)` |
| `Remove(entity)` | Soft-deletes `SoftDeletableEntity`; hard-deletes others |
| `UnitOfWork` | Returns the `ApplicationDbContext` as `IUnitOfWork` |
| `DbItems` | `context.Set<TEntity>().AsQueryable()` — use as base for custom queries |

**Only add methods to the concrete class** that are declared in the `I{AggregateName}Repository` interface and cannot be satisfied by the base.

---

## DependencyInjection.cs Registration

Open `src/ProjectName.Infrastructure/DependencyInjection.cs` and add the registration inside the `AddCustomServices()` private method, alongside the other concrete repository registrations:

```csharp
services.AddScoped<I{AggregateName}Repository, {AggregateName}Repository>();
```

Place it grouped with the other repository registrations (after the generic `IRepository<,>` registration).

---

## Important Rules

- Use `DbItems` (the protected `IQueryable<TEntity>`) as the starting point for LINQ queries — never `context.Set<TEntity>()` directly in the concrete class
- Always filter `x.DeletedAt == null` in custom queries on soft-deletable entities
- Always use `CancellationToken cancellationToken = default` as the last parameter on async methods
- The constructor must use **primary constructor syntax**: `{AggregateName}Repository(ApplicationDbContext context)` — no field declarations needed; pass `context` to `base(context)`
- Never override `FindByIdAsync` unless the domain requires eager-loading navigation properties (and even then, prefer a separate explicit method)

## Namespace and File Placement

- File: `src/ProjectName.Infrastructure/Repositories/{AggregateName}Repository.cs`
- Namespace: `ProjectName.Infrastructure.Repositories`

## Checklist Before Finishing

- [ ] Class uses primary constructor syntax and calls `base(context)`
- [ ] Class extends `Repository<{AggregateName}, {KeyType}>` and implements `I{AggregateName}Repository`
- [ ] Only methods declared in the interface are added — no extra methods
- [ ] Custom query methods use `DbItems` not `context.Set<T>()`
- [ ] All custom queries filter `DeletedAt == null` for soft-deletable entities
- [ ] `DependencyInjection.cs` has `services.AddScoped<I{AggregateName}Repository, {AggregateName}Repository>()`
