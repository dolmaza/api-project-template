---
name: add-domain-aggregate
description: Generate a new domain aggregate in src/ProjectName.Domain/ with all required files (aggregate root, domain errors, repository interface, status enum, optional child entities) following this codebase's DDD conventions. Use when the user asks to add, create, or scaffold a new domain aggregate.
allowed-tools: Read Write Glob Grep
---

You are an expert in this codebase's Domain-Driven Design patterns. When given a description of a new aggregate, generate all necessary files in `src/ProjectName.Domain/AggregatesModel/` following the exact conventions established by the existing aggregates (Product, Prescription, Pharmacy).

## User's Request

$ARGUMENTS

---

## What to Generate

For a new aggregate named `{AggregateName}`, create these files under `src/ProjectName.Domain/AggregatesModel/{AggregateName}Aggregate/`:

1. `{AggregateName}.cs` — Aggregate root entity
2. `{AggregateName}DomainErrors.cs` — Static error constants
3. `{AggregateName}Status.cs` — Status enum (if the aggregate has a lifecycle)
4. `I{AggregateName}Repository.cs` — Repository interface
5. `{ChildEntity}.cs` — Child entity (if the aggregate has child entities)
6. `{ChildEntity}DomainErrors.cs` — Child entity errors (if child entities exist)

---

## Conventions and Patterns

### 1. Aggregate Root Class

```csharp
namespace ProjectName.Domain.AggregatesModel.{AggregateName}Aggregate;

public class {AggregateName} : SoftDeletableEntity<{KeyType}>, IAggregateRoot
{
    // ── Constants ──────────────────────────────────────────────────────────
    public const int NameMaxLength = 200;  // define per-property max lengths as const int

    // ── Properties ─────────────────────────────────────────────────────────
    public string Name { get; private set; } = null!;
    public {AggregateName}Status Status { get; private set; }
    // ... other properties with private setters

    // ── Child collections (if any) ─────────────────────────────────────────
    private readonly List<ChildEntity> _items = [];
    public IReadOnlyCollection<ChildEntity> Items => _items.AsReadOnly();

    // ── Parameterless constructor required by EF Core ──────────────────────
    private {AggregateName}() { }

    // ── Factory method ─────────────────────────────────────────────────────
    public static Result<{AggregateName}> Create(string name, {AggregateName}Status status = {AggregateName}Status.Active)
    {
        // 1. Validate all inputs using ValidationHelpers
        var nameResult = ValidationHelpers.ValidateRequiredWithMaxLength(
            name, NameMaxLength,
            {AggregateName}DomainErrors.Name.Required,
            {AggregateName}DomainErrors.Name.TooLong);
        if (nameResult.IsFailure) return nameResult;

        // 2. Construct and return entity
        return new {AggregateName}
        {
            Name = name.Trim(),
            Status = status
        };
    }

    // ── Mutation methods ───────────────────────────────────────────────────
    public Result Update(string name)
    {
        // Always check soft-delete first
        var notDeletedResult = ValidateNotDeleted({AggregateName}DomainErrors.AlreadyDeleted);
        if (notDeletedResult.IsFailure) return notDeletedResult;

        var nameResult = ValidationHelpers.ValidateRequiredWithMaxLength(
            name, NameMaxLength,
            {AggregateName}DomainErrors.Name.Required,
            {AggregateName}DomainErrors.Name.TooLong);
        if (nameResult.IsFailure) return nameResult;

        Name = name.Trim();
        return Result.Success();
    }

    public Result ChangeStatus({AggregateName}Status newStatus)
    {
        var notDeletedResult = ValidateNotDeleted({AggregateName}DomainErrors.AlreadyDeleted);
        if (notDeletedResult.IsFailure) return notDeletedResult;

        var validStatus = ValidationHelpers.ValidateEnum(newStatus, {AggregateName}DomainErrors.Status.Invalid);
        if (validStatus.IsFailure) return validStatus;

        Status = newStatus;
        return Result.Success();
    }
}
```

**Rules:**
- Inherit `SoftDeletableEntity<TKey>` (use `int` for smaller aggregates, `long` for high-volume) and `IAggregateRoot`
- All property setters are `private`
- Child collections: `private readonly List<T>` exposed as `IReadOnlyCollection<T>`
- EF Core requires a `private` parameterless constructor
- Factory method is `public static Result<{AggregateName}> Create(...)`
- Every mutating method calls `ValidateNotDeleted(...)` first
- All input validation uses `ValidationHelpers` static methods
- All methods return `Result` or `Result<T>` — never throw for business rule violations
- Use `AddDomainEvent(...)` for significant state transitions (creation, status change, deletion)

### 2. Domain Errors Class

```csharp
namespace ProjectName.Domain.AggregatesModel.{AggregateName}Aggregate;

public static class {AggregateName}DomainErrors
{
    public static readonly Error AlreadyDeleted =
        Error.Conflict("{AggregateName}.AlreadyDeleted", "This {aggregateName} has already been deleted.");

    public static class Name
    {
        public static readonly Error Required =
            Error.Validation("{AggregateName}.Name.Required", "{AggregateName} name is required.");

        public static readonly Error TooLong =
            Error.Validation("{AggregateName}.Name.TooLong",
                $"{AggregateName} name must not exceed {{{AggregateName}.NameMaxLength}} characters.");
    }

    public static class Status
    {
        public static readonly Error Invalid =
            Error.Validation("{AggregateName}.Status.Invalid", "Invalid {aggregateName} status.");
    }

    // Add a nested static class per property that has validation rules
}
```

**Rules:**
- Outer class holds aggregate-level errors (`AlreadyDeleted`)
- Nested static classes group errors per property (`Name`, `Status`, `Email`, etc.)
- Error code format: `{AggregateName}.{Property}.{ErrorType}` (e.g., `Pharmacy.Name.Required`)
- `Error.Validation(...)` — input validation failures
- `Error.Conflict(...)` — state conflicts (duplicate, already deleted)
- `Error.NotFound(...)` — lookup failures
- `Error.Failure(...)` — unexpected/generic failures

### 3. Status Enum (only if aggregate has a lifecycle)

```csharp
namespace ProjectName.Domain.AggregatesModel.{AggregateName}Aggregate;

public enum {AggregateName}Status
{
    Active = 0,
    Inactive = 1,
    // add domain-specific statuses as needed
}
```

### 4. Repository Interface

```csharp
namespace ProjectName.Domain.AggregatesModel.{AggregateName}Aggregate;

public interface I{AggregateName}Repository : IRepository<{AggregateName}, {KeyType}>
{
    // Add domain-specific query methods only when needed, e.g.:
    Task<{AggregateName}?> FindByNameAsync(string name, CancellationToken cancellationToken = default);
}
```

**Rules:**
- Extends `IRepository<{AggregateName}, {KeyType}>` from `ProjectName.Domain.SeedWork`
- Only add methods that cannot be expressed via `FindByIdAsync` + in-memory filtering
- Always use `CancellationToken cancellationToken = default` as the last parameter

### 5. Child Entity (only if the aggregate has child entities)

```csharp
namespace ProjectName.Domain.AggregatesModel.{AggregateName}Aggregate;

public class {ChildEntity} : Entity<long>
{
    public const int CaptionMaxLength = 500;

    public {KeyType} {AggregateName}Id { get; private set; }
    public string Caption { get; private set; } = null!;

    private {ChildEntity}() { }

    public static Result<{ChildEntity}> Create({KeyType} parentId, string caption)
    {
        var captionResult = ValidationHelpers.ValidateRequiredWithMaxLength(
            caption, CaptionMaxLength,
            {ChildEntity}DomainErrors.Caption.Required,
            {ChildEntity}DomainErrors.Caption.TooLong);
        if (captionResult.IsFailure) return captionResult;

        return new {ChildEntity}
        {
            {AggregateName}Id = parentId,
            Caption = caption.Trim()
        };
    }

    public Result UpdateCaption(string caption)
    {
        var result = ValidationHelpers.ValidateRequiredWithMaxLength(
            caption, CaptionMaxLength,
            {ChildEntity}DomainErrors.Caption.Required,
            {ChildEntity}DomainErrors.Caption.TooLong);
        if (result.IsFailure) return result;

        Caption = caption.Trim();
        return Result.Success();
    }
}
```

**Rules:**
- Child entities inherit `Entity<TKey>` only — NOT `IAggregateRoot`
- Always include FK property pointing back to the parent aggregate
- Same factory method + Result pattern as aggregate roots
- Do NOT soft-delete child entities directly — manage via aggregate root methods

---

## ValidationHelpers Reference

All methods are static, in `ProjectName.Domain.Common.Validation.ValidationHelpers`:

| Method | Use when |
|---|---|
| `ValidateRequired(value, error)` | string must not be null/whitespace |
| `ValidateMaxLength(value, max, error)` | string length limit |
| `ValidateRequiredWithMaxLength(value, max, reqErr, lenErr)` | required + max length |
| `ValidateUrl(value, max, formatErr, lenErr)` | URL format + length |
| `ValidateRange(value, min, max, error)` | numeric range check |
| `ValidateGuidNotEmpty(value, error)` | Guid not empty/default |
| `ValidateKeyFormat(value, max, error)` | lowercase alphanumeric + underscore |
| `ValidateEnum<TEnum>(value, error)` | enum is a defined value |
| `ValidateAddress(value, error)` | Address value object |

---

## File Placement and Namespace

- Directory: `src/ProjectName.Domain/AggregatesModel/{AggregateName}Aggregate/`
- Namespace: `ProjectName.Domain.AggregatesModel.{AggregateName}Aggregate`
- Required usings (add only what's used):
  ```csharp
  using ProjectName.Domain.Common.ResultPattern;
  using ProjectName.Domain.Common.Validation;
  using ProjectName.Domain.SeedWork;
  ```

---

## Checklist Before Finishing

- [ ] Aggregate root inherits `SoftDeletableEntity<TKey>` and `IAggregateRoot`
- [ ] Private parameterless constructor exists for EF Core
- [ ] All properties have `private set`
- [ ] Factory method returns `Result<{AggregateName}>`
- [ ] Every mutation method checks `ValidateNotDeleted(...)` first
- [ ] All validations use `ValidationHelpers` methods
- [ ] All domain error codes follow `{AggregateName}.{Property}.{ErrorType}` format
- [ ] Repository interface extends `IRepository<{AggregateName}, {KeyType}>`
- [ ] Child entities inherit `Entity<TKey>` (not `IAggregateRoot`)
- [ ] No exceptions thrown for business rule violations — only `Result.Failure(...)`
- [ ] No public setters anywhere
