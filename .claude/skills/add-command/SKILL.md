---
name: add-command
description: Scaffold a command, its handler, and FluentValidation validator for an existing aggregate in the Application layer. Use when the user asks to add a Create, Update, Delete, or any other mutating operation to an existing aggregate.
allowed-tools: Read Write Glob Grep
---

You are an expert in this codebase's CQRS patterns. Given an aggregate name and a description of the operation, generate the command, handler, and validator files in `src/ProjectName.Application/{Domain}/Commands/{OperationName}/`.

## User's Request

$ARGUMENTS

---

## What to Generate

For a command named `{OperationName}{AggregateName}Command`, create these files under `src/ProjectName.Application/{Domain}/Commands/{OperationName}/`:

1. `{OperationName}{AggregateName}Command.cs` — command record + handler class (in the same file)
2. `{OperationName}{AggregateName}CommandValidator.cs` — FluentValidation validator

If `{Domain}Errors.cs` does not yet exist at `src/ProjectName.Application/{Domain}/{Domain}Errors.cs`, create it too.

---

## Patterns by Operation Type

### CREATE command — returns `Result<TKey>`

```csharp
using ProjectName.Domain.AggregatesModel.{Domain}Aggregate;
using ProjectName.Domain.Common.Abstractions;
using ProjectName.Domain.Common.ResultPattern;

namespace ProjectName.Application.{Domain}.Commands.Create;

public record Create{AggregateName}Command(
    string Name,
    // ... other input fields
) : ICommand<Result<{KeyType}>>;

public class Create{AggregateName}CommandHandler(I{AggregateName}Repository repository)
    : ICommandHandler<Create{AggregateName}Command, Result<{KeyType}>>
{
    public async Task<Result<{KeyType}>> Handle(Create{AggregateName}Command request, CancellationToken cancellationToken)
    {
        var result = {AggregateName}.Create(request.Name /*, ...other fields */);
        if (result.IsFailure)
            return Result<{KeyType}>.Failure(result.Error);

        await repository.AddAsync(result.Value, cancellationToken);
        await repository.UnitOfWork.SaveEntitiesAsync(cancellationToken);

        return Result<{KeyType}>.Success(result.Value.Id);
    }
}
```

### UPDATE command — returns `Result`

```csharp
using ProjectName.Domain.AggregatesModel.{Domain}Aggregate;
using ProjectName.Domain.Common.Abstractions;
using ProjectName.Domain.Common.ResultPattern;

namespace ProjectName.Application.{Domain}.Commands.Update;

public record Update{AggregateName}Command(
    {KeyType} Id,
    string Name
    // ... other updatable fields
) : ICommand<Result>;

public class Update{AggregateName}CommandHandler(I{AggregateName}Repository repository)
    : ICommandHandler<Update{AggregateName}Command, Result>
{
    public async Task<Result> Handle(Update{AggregateName}Command request, CancellationToken cancellationToken)
    {
        var entity = await repository.FindByIdAsync(request.Id);
        if (entity is null)
            return {Domain}Errors.{AggregateName}NotFound;

        var updateResult = entity.Update(request.Name /*, ...other fields */);
        if (updateResult.IsFailure)
            return updateResult;

        repository.Update(entity);
        await repository.UnitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
```

### DELETE command — returns `Result`

```csharp
using ProjectName.Domain.AggregatesModel.{Domain}Aggregate;
using ProjectName.Domain.Common.Abstractions;
using ProjectName.Domain.Common.ResultPattern;

namespace ProjectName.Application.{Domain}.Commands.Delete;

public record Delete{AggregateName}Command({KeyType} Id) : ICommand<Result>;

public class Delete{AggregateName}CommandHandler(I{AggregateName}Repository repository)
    : ICommandHandler<Delete{AggregateName}Command, Result>
{
    public async Task<Result> Handle(Delete{AggregateName}Command request, CancellationToken cancellationToken)
    {
        var entity = await repository.FindByIdAsync(request.Id);
        if (entity is null)
            return {Domain}Errors.{AggregateName}NotFound;

        repository.Remove(entity);
        await repository.UnitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
```

---

## Validator Pattern

```csharp
using FluentValidation;
using ProjectName.Domain.AggregatesModel.{Domain}Aggregate;

namespace ProjectName.Application.{Domain}.Commands.{OperationName};

public class {OperationName}{AggregateName}CommandValidator : AbstractValidator<{OperationName}{AggregateName}Command>
{
    public {OperationName}{AggregateName}CommandValidator()
    {
        // For ID fields in Update/Delete:
        RuleFor(x => x.Id).NotEmpty();

        // For required string fields:
        RuleFor(x => x.Name)
            .NotEmpty()
            .MaximumLength({AggregateName}.NameMaxLength);

        // For optional string fields:
        RuleFor(x => x.Description)
            .MaximumLength({AggregateName}.DescriptionMaxLength);
    }
}
```

**Rules:**
- Mirror domain max-length constants (e.g., `{AggregateName}.NameMaxLength`) — never hardcode numbers
- If the domain aggregate has a `NameMaxLength` const, use it in the validator
- Do not add validators for computed fields or IDs generated by the system (like the returned ID from Create)

---

## Application Errors File

```csharp
using ProjectName.Domain.Common.ResultPattern;

namespace ProjectName.Application.{Domain};

public static class {Domain}Errors
{
    public static readonly Error {AggregateName}NotFound =
        Error.NotFound("{AggregateName}.NotFound", "Couldn't find {aggregateName} with provided id.");
}
```

---

## Important Rules

- Command record and handler class live in the **same** `.cs` file
- Command is a `record`, handler is a `class`
- **Create handlers**: call `repository.UnitOfWork.SaveEntitiesAsync(cancellationToken)` — dispatches domain events
- **Update/Delete handlers**: call `repository.UnitOfWork.SaveChangesAsync(cancellationToken)` — unless the operation raises domain events, then use `SaveEntitiesAsync`
- Always check `FindByIdAsync` result for null in Update/Delete and return the `NotFound` error
- Always propagate `IsFailure` results from domain method calls immediately
- Never throw exceptions for business rule failures — only `Result.Failure(...)`
- The `{Domain}Errors.cs` file holds application-level errors (NotFound); domain errors stay in the domain project

## Namespace and File Placement

- Directory: `src/ProjectName.Application/{Domain}/Commands/{OperationName}/`
- Namespace: `ProjectName.Application.{Domain}.Commands.{OperationName}`
- `{Domain}Errors.cs`: `src/ProjectName.Application/{Domain}/{Domain}Errors.cs`

## Checklist Before Finishing

- [ ] Command is a `record` implementing `ICommand<Result>` or `ICommand<Result<TKey>>`
- [ ] Handler class constructor uses primary constructor syntax with repository injected
- [ ] Create handler uses `SaveEntitiesAsync`; Update/Delete use `SaveChangesAsync` (unless domain events needed)
- [ ] Update/Delete handlers perform null check and return `NotFound` error
- [ ] Domain method failures are immediately propagated with `return result` or `return Result.Failure(result.Error)`
- [ ] Validator mirrors domain aggregate max-length constants
- [ ] `{Domain}Errors.cs` exists with `{AggregateName}NotFound` error
- [ ] All files are in the correct directory with correct namespace
