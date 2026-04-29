---
name: add-unit-tests
description: Generate a comprehensive unit test suite for a domain aggregate or command handler — covering happy path, all validation failure branches, soft-delete guard, and persistence verification. Use when the user asks to add tests for a new aggregate or command.
allowed-tools: Read Write Glob Grep
---

You are an expert in this codebase's xUnit + Moq + FluentAssertions testing patterns. Read the target aggregate/command handler source files and generate a complete test suite covering every code path.

## User's Request

$ARGUMENTS

---

## What to Generate

For a **domain aggregate**:
- `tests/ProjectName.UnitTests/Domain/{AggregateName}Aggregate/{AggregateName}Tests.cs`

For **command handlers** (generate all three if CRUD commands exist):
- `tests/ProjectName.UnitTests/Application/{Domain}/Commands/Create{AggregateName}CommandHandlerTests.cs`
- `tests/ProjectName.UnitTests/Application/{Domain}/Commands/Update{AggregateName}CommandHandlerTests.cs`
- `tests/ProjectName.UnitTests/Application/{Domain}/Commands/Delete{AggregateName}CommandHandlerTests.cs`

---

## Domain Aggregate Test Template

```csharp
using FluentAssertions;
using ProjectName.Domain.AggregatesModel.{Domain}Aggregate;

namespace ProjectName.UnitTests.Domain.{AggregateName}Aggregate;

public class {AggregateName}Tests
{
    // ── Create factory method ─────────────────────────────────────────────

    [Fact]
    public void Create_ShouldSucceed_WithValidInput()
    {
        var result = {AggregateName}.Create("  Valid Name  " /*, other valid args */);

        result.IsSuccess.Should().BeTrue();
        result.Value.Name.Should().Be("Valid Name");   // verify trimming
        result.Value.Status.Should().Be({AggregateName}Status.Active);
    }

    [Fact]
    public void Create_ShouldFail_WhenNameIsEmpty()
    {
        var result = {AggregateName}.Create("   " /*, other args */);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be({AggregateName}DomainErrors.NameRequired);
    }

    [Fact]
    public void Create_ShouldFail_WhenNameExceedsMaxLength()
    {
        var longName = new string('x', {AggregateName}.NameMaxLength + 1);

        var result = {AggregateName}.Create(longName /*, other args */);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be({AggregateName}DomainErrors.NameTooLong);
    }

    // ── Update mutation method ────────────────────────────────────────────

    [Fact]
    public void Update_ShouldSucceed_AndMutateFields()
    {
        var entity = CreateValid{AggregateName}();

        var result = entity.Update("  New Name  " /*, other fields */);

        result.IsSuccess.Should().BeTrue();
        entity.Name.Should().Be("New Name");
    }

    [Fact]
    public void Update_ShouldFail_WhenAlreadyDeleted()
    {
        var entity = CreateValid{AggregateName}();
        var initialName = entity.Name;
        entity.MarkAsDeleted();

        var result = entity.Update("New Name");

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be({AggregateName}DomainErrors.AlreadyDeleted);
        entity.Name.Should().Be(initialName); // state must be unchanged
    }

    // ── ChangeStatus mutation method ──────────────────────────────────────

    [Fact]
    public void ChangeStatus_ShouldSucceed_WithValidStatus()
    {
        var entity = CreateValid{AggregateName}();

        var result = entity.ChangeStatus({AggregateName}Status.Inactive);

        result.IsSuccess.Should().BeTrue();
        entity.Status.Should().Be({AggregateName}Status.Inactive);
    }

    [Fact]
    public void ChangeStatus_ShouldFail_WhenStatusIsInvalid()
    {
        var entity = CreateValid{AggregateName}();

        var result = entity.ChangeStatus(({AggregateName}Status)999);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be({AggregateName}DomainErrors.InvalidStatus);
    }

    [Fact]
    public void ChangeStatus_ShouldFail_WhenAlreadyDeleted()
    {
        var entity = CreateValid{AggregateName}();
        entity.MarkAsDeleted();

        var result = entity.ChangeStatus({AggregateName}Status.Inactive);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be({AggregateName}DomainErrors.AlreadyDeleted);
    }

    // ── Helper ────────────────────────────────────────────────────────────

    private static {AggregateName} CreateValid{AggregateName}() =>
        {AggregateName}.Create("Valid Name" /*, other valid args */).Value;
}
```

---

## Create Command Handler Test Template

```csharp
using FluentAssertions;
using Moq;
using ProjectName.Application.{Domain}.Commands.Create;
using ProjectName.Domain.AggregatesModel.{Domain}Aggregate;
using ProjectName.Domain.SeedWork;

namespace ProjectName.UnitTests.Application.{Domain}.Commands;

public class Create{AggregateName}CommandHandlerTests
{
    [Fact]
    public async Task Handle_ShouldCreate_AndPersist_WhenRequestIsValid()
    {
        var unitOfWorkMock = new Mock<IUnitOfWork>();
        unitOfWorkMock
            .Setup(uow => uow.SaveEntitiesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var repositoryMock = new Mock<I{AggregateName}Repository>();
        repositoryMock.SetupGet(r => r.UnitOfWork).Returns(unitOfWorkMock.Object);

        var command = new Create{AggregateName}Command("Valid Name" /*, other valid fields */);
        var handler = new Create{AggregateName}CommandHandler(repositoryMock.Object);

        var result = await handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();

        repositoryMock.Verify(
            r => r.AddAsync(
                It.Is<{AggregateName}>(e => e.Name == "Valid Name"),
                It.IsAny<CancellationToken>()),
            Times.Once);

        unitOfWorkMock.Verify(uow => uow.SaveEntitiesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_ShouldReturnFailure_AndNotPersist_WhenDomainValidationFails()
    {
        var repositoryMock = new Mock<I{AggregateName}Repository>();
        repositoryMock.SetupGet(r => r.UnitOfWork).Returns(new Mock<IUnitOfWork>().Object);

        var command = new Create{AggregateName}Command("   " /*, other fields */);
        var handler = new Create{AggregateName}CommandHandler(repositoryMock.Object);

        var result = await handler.Handle(command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be({AggregateName}DomainErrors.NameRequired);

        repositoryMock.Verify(
            r => r.AddAsync(It.IsAny<{AggregateName}>(), It.IsAny<CancellationToken>()),
            Times.Never);
        repositoryMock.Verify(
            r => r.UnitOfWork.SaveEntitiesAsync(It.IsAny<CancellationToken>()),
            Times.Never);
    }
}
```

---

## Update Command Handler Test Template

```csharp
public class Update{AggregateName}CommandHandlerTests
{
    [Fact]
    public async Task Handle_ShouldReturnNotFound_WhenEntityDoesNotExist()
    {
        var repositoryMock = new Mock<I{AggregateName}Repository>();
        repositoryMock.SetupGet(r => r.UnitOfWork).Returns(new Mock<IUnitOfWork>().Object);
        repositoryMock.Setup(r => r.FindByIdAsync(It.IsAny<{KeyType}>())).ReturnsAsync(({AggregateName}?)null);

        var command = new Update{AggregateName}Command(1 /*, valid fields */);
        var handler = new Update{AggregateName}CommandHandler(repositoryMock.Object);

        var result = await handler.Handle(command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be({Domain}Errors.{AggregateName}NotFound);

        repositoryMock.Verify(r => r.Update(It.IsAny<{AggregateName}>()), Times.Never);
    }

    [Fact]
    public async Task Handle_ShouldReturnFailure_WhenDomainValidationFails()
    {
        var entity = Create{AggregateName}();
        var repositoryMock = new Mock<I{AggregateName}Repository>();
        repositoryMock.SetupGet(r => r.UnitOfWork).Returns(new Mock<IUnitOfWork>().Object);
        repositoryMock.Setup(r => r.FindByIdAsync(entity.Id)).ReturnsAsync(entity);

        var command = new Update{AggregateName}Command(entity.Id, "   " /*, other fields */);
        var handler = new Update{AggregateName}CommandHandler(repositoryMock.Object);

        var result = await handler.Handle(command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be({AggregateName}DomainErrors.NameRequired);

        repositoryMock.Verify(r => r.Update(It.IsAny<{AggregateName}>()), Times.Never);
    }

    [Fact]
    public async Task Handle_ShouldUpdateAndPersist_WhenRequestIsValid()
    {
        var entity = Create{AggregateName}();
        var unitOfWorkMock = new Mock<IUnitOfWork>();
        var repositoryMock = new Mock<I{AggregateName}Repository>();
        repositoryMock.SetupGet(r => r.UnitOfWork).Returns(unitOfWorkMock.Object);
        repositoryMock.Setup(r => r.FindByIdAsync(entity.Id)).ReturnsAsync(entity);

        var command = new Update{AggregateName}Command(entity.Id, "Updated Name" /*, valid fields */);
        var handler = new Update{AggregateName}CommandHandler(repositoryMock.Object);

        var result = await handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();

        repositoryMock.Verify(
            r => r.Update(It.Is<{AggregateName}>(e => e.Name == "Updated Name")),
            Times.Once);
        unitOfWorkMock.Verify(uow => uow.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    private static {AggregateName} Create{AggregateName}() =>
        {AggregateName}.Create("Original Name").Value;
}
```

---

## Delete Command Handler Test Template

```csharp
public class Delete{AggregateName}CommandHandlerTests
{
    [Fact]
    public async Task Handle_ShouldReturnNotFound_WhenEntityDoesNotExist()
    {
        var repositoryMock = new Mock<I{AggregateName}Repository>();
        repositoryMock.SetupGet(r => r.UnitOfWork).Returns(new Mock<IUnitOfWork>().Object);
        repositoryMock.Setup(r => r.FindByIdAsync(It.IsAny<{KeyType}>())).ReturnsAsync(({AggregateName}?)null);

        var result = await new Delete{AggregateName}CommandHandler(repositoryMock.Object)
            .Handle(new Delete{AggregateName}Command(1), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be({Domain}Errors.{AggregateName}NotFound);

        repositoryMock.Verify(r => r.Remove(It.IsAny<{AggregateName}>()), Times.Never);
    }

    [Fact]
    public async Task Handle_ShouldRemoveAndPersist_WhenEntityExists()
    {
        var entity = {AggregateName}.Create("Name").Value;
        var unitOfWorkMock = new Mock<IUnitOfWork>();
        var repositoryMock = new Mock<I{AggregateName}Repository>();
        repositoryMock.SetupGet(r => r.UnitOfWork).Returns(unitOfWorkMock.Object);
        repositoryMock.Setup(r => r.FindByIdAsync(entity.Id)).ReturnsAsync(entity);

        var result = await new Delete{AggregateName}CommandHandler(repositoryMock.Object)
            .Handle(new Delete{AggregateName}Command(entity.Id), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();

        repositoryMock.Verify(r => r.Remove(entity), Times.Once);
        unitOfWorkMock.Verify(uow => uow.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }
}
```

---

## Test Writing Rules

- **Read the actual source files** before generating tests — test the real error constants and field names
- Test method naming: `MethodName_ShouldExpectedBehavior_WhenCondition`
- Use `result.Error.Should().Be(ActualErrorConstant)` — never compare error codes as strings
- **Always verify** that `AddAsync` / `Update` / `Remove` are called `Times.Once` on success
- **Always verify** that persistence methods are called `Times.Never` on failure
- For Create handlers, verify `SaveEntitiesAsync`; for Update/Delete, verify `SaveChangesAsync`
- Use `It.Is<TEntity>(predicate)` to assert the entity state passed to `AddAsync` / `Update`
- Keep a `private static {AggregateName} CreateValid{AggregateName}()` helper to avoid repetition
- Domain tests: assert state is **unchanged** when a mutation returns failure
- Never mock the domain aggregate itself — use the real aggregate class

## Namespace and File Placement

- Domain tests: `tests/ProjectName.UnitTests/Domain/{AggregateName}Aggregate/`
- Application tests: `tests/ProjectName.UnitTests/Application/{Domain}/Commands/`
- Namespace mirrors the path: `ProjectName.UnitTests.Domain.{AggregateName}Aggregate`

## Checklist Before Finishing

- [ ] Domain tests cover: valid `Create`, each validation failure in `Create`, each mutation method (success + failure + soft-delete guard)
- [ ] Create handler tests cover: success (with persistence verification) + domain validation failure (no persistence)
- [ ] Update handler tests cover: not found + domain validation failure + success (with `repository.Update` verification)
- [ ] Delete handler tests cover: not found + success (with `repository.Remove` verification)
- [ ] All error comparisons use actual error constant references (e.g., `PharmacyDomainErrors.NameRequired`)
- [ ] Success cases verify both the repository method and `SaveChangesAsync`/`SaveEntitiesAsync` are called `Times.Once`
- [ ] Failure cases verify repository and unit of work methods are `Times.Never`
- [ ] `CreateValid{AggregateName}()` helper exists and is reused across test methods
