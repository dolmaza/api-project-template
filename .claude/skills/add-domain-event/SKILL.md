---
name: add-domain-event
description: Scaffold a domain event, raise it inside an aggregate mutation, create the IDomainEventHandler in Infrastructure, and register the handler in DependencyInjection.cs. Use when the user asks to add a side effect triggered by an aggregate state change (e.g., notifications, integration events, audit trails).
allowed-tools: Read Write Glob Grep
---

You are an expert in this codebase's domain event patterns. Given an aggregate state change and the desired side effect, generate all files and wiring needed for a domain event.

## User's Request

$ARGUMENTS

---

## What to Generate

1. `src/ProjectName.Domain/AggregatesModel/{Domain}Aggregate/{EventName}Event.cs` — the event class
2. Modify the aggregate mutation method to call `AddDomainEvent(new {EventName}Event(...))`
3. `src/ProjectName.Infrastructure/DomainEventHandlers/{EventName}EventHandler.cs` — the handler
4. Update `src/ProjectName.Infrastructure/DependencyInjection.cs` — register the handler

---

## Domain Event Class Template

```csharp
using ProjectName.Domain.Common.DomainEvents;

namespace ProjectName.Domain.AggregatesModel.{Domain}Aggregate;

public sealed class {EventName}Event : IDomainEvent
{
    // Include all data the handler will need — denormalized for loose coupling
    public {KeyType} {AggregateName}Id { get; }
    public string UserId { get; }
    public {AggregateName}Status OldStatus { get; }    // include if status change
    public {AggregateName}Status NewStatus { get; }    // include if status change
    public DateTime OccurredOn { get; }

    public {EventName}Event(
        {KeyType} {aggregateName}Id,
        string userId,
        {AggregateName}Status oldStatus,
        {AggregateName}Status newStatus)
    {
        {AggregateName}Id = {aggregateName}Id;
        UserId = userId;
        OldStatus = oldStatus;
        NewStatus = newStatus;
        OccurredOn = DateTime.UtcNow;
    }
}
```

**Rules:**
- Class is `sealed`
- Implements `IDomainEvent` (requires `DateTime OccurredOn { get; }`)
- All properties are init-only (`{ get; }`) — no setters
- `OccurredOn` is set to `DateTime.UtcNow` in the constructor
- Include only the data needed by handlers; avoid passing the full aggregate object

---

## Raising the Event in the Aggregate

Find the mutation method in the aggregate that triggers this event and call `AddDomainEvent(...)` **after** state has been updated:

```csharp
public Result ChangeStatus({AggregateName}Status newStatus)
{
    var notDeletedResult = ValidateNotDeleted({AggregateName}DomainErrors.AlreadyDeleted);
    if (notDeletedResult.IsFailure) return notDeletedResult;

    var validStatus = ValidationHelpers.ValidateEnum(newStatus, {AggregateName}DomainErrors.Status.Invalid);
    if (validStatus.IsFailure) return validStatus;

    var oldStatus = Status;   // capture before mutation
    Status = newStatus;

    // Raise the event AFTER state is updated
    AddDomainEvent(new {EventName}Event(Id, UserId, oldStatus, newStatus));

    return Result.Success();
}
```

**Key:** Call `AddDomainEvent(...)` after state mutation, not before. The event represents a fact that has occurred.

---

## Domain Event Handler Template

```csharp
using Microsoft.Extensions.Logging;
using ProjectName.Domain.AggregatesModel.{Domain}Aggregate;
using ProjectName.Domain.Common.DomainEvents;

namespace ProjectName.Infrastructure.DomainEventHandlers;

public class {EventName}EventHandler(
    I{ServiceName} service,
    ILogger<{EventName}EventHandler> logger)
    : IDomainEventHandler<{EventName}Event>
{
    public async Task Handle({EventName}Event domainEvent, CancellationToken cancellationToken = default)
    {
        logger.LogInformation(
            "{AggregateName} {Id} — {EventDescription}",
            domainEvent.{AggregateName}Id,
            /* other relevant fields */);

        await service.{DoSomethingAsync}(
            domainEvent.{AggregateName}Id,
            domainEvent.NewStatus,
            cancellationToken);
    }
}
```

**Rules:**
- Handler is in `ProjectName.Infrastructure.DomainEventHandlers` namespace
- Uses primary constructor injection
- Always includes `ILogger<{EventName}EventHandler>` for structured logging
- Keep the handler thin — delegate actual work to an injected service
- The handler runs inside the same DB transaction as the command (dispatched in `SaveEntitiesAsync`)

---

## DI Registration

In `src/ProjectName.Infrastructure/DependencyInjection.cs`, inside `AddCustomServices()`, add:

```csharp
services.AddScoped<
    IDomainEventHandler<{EventName}Event>,
    {EventName}EventHandler>();
```

Place alongside the other `IDomainEventHandler` registrations.

---

## Event Dispatch Flow (for reference)

```
Command handler → repository.UnitOfWork.SaveEntitiesAsync()
  → EF SaveChanges (persists state)
  → mediator.DispatchDomainEventsAsync(dbContext)
    → for each entity with domain events:
        → IDomainEventHandler<TEvent>.Handle(event)
```

This means:
- The handler runs **after** the state is persisted to the DB
- The handler runs **within** the open `TransactionBehaviour` transaction
- If the handler throws, the transaction rolls back (both the aggregate state change and the side effect)

---

## Checklist Before Finishing

- [ ] Event class is `sealed` and implements `IDomainEvent`
- [ ] All event properties are `{ get; }` (no setters)
- [ ] `OccurredOn` is set to `DateTime.UtcNow` in the constructor
- [ ] `AddDomainEvent(...)` is called after the state mutation in the aggregate
- [ ] Event data is self-contained (no navigation to aggregate object)
- [ ] Handler is in `ProjectName.Infrastructure.DomainEventHandlers` namespace
- [ ] Handler uses primary constructor injection and includes `ILogger`
- [ ] Handler delegates work to an injected service — no direct DB access in the handler
- [ ] `DependencyInjection.cs` has `services.AddScoped<IDomainEventHandler<{EventName}Event>, {EventName}EventHandler>()`
