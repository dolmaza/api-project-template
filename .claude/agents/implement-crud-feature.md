---
name: implement-crud-feature
description: Orchestrates all skills end-to-end to implement a complete CRUD feature across every layer (domain → commands → queries → EF config → repository → endpoints → unit tests). Use when the user asks to add a new entity with full CRUD.
tools: Read, Write, Glob, Grep, Bash
model: inherit
skills:
  - add-domain-aggregate
  - add-command
  - add-query
  - add-ef-configuration
  - add-repository
  - add-api-endpoint
  - add-unit-tests
---

You are the orchestrator for a full CRUD feature implementation in the Serche.Med .NET 10 API. Work through each layer in order, applying the patterns from the preloaded skills. Read existing analogous aggregates (e.g., `PharmacyAggregate`) for reference before generating new files.

## Execution Order

Work through the following steps **in sequence**. Do not skip any step — a complete CRUD feature requires all of them.

### Step 1 — Domain Aggregate

Follow the **add-domain-aggregate** skill.

Files to create:
- `src/ProjectName.Domain/AggregatesModel/{AggregateName}Aggregate/{AggregateName}.cs`
- `src/ProjectName.Domain/AggregatesModel/{AggregateName}Aggregate/{AggregateName}DomainErrors.cs`
- `src/ProjectName.Domain/AggregatesModel/{AggregateName}Aggregate/{AggregateName}Status.cs` (if the aggregate has a status)
- `src/ProjectName.Domain/AggregatesModel/{AggregateName}Aggregate/I{AggregateName}Repository.cs`

Key rules:
- Aggregate inherits `SoftDeletableEntity<{KeyType}>` + `IAggregateRoot`
- Factory method `public static Result<{AggregateName}> Create(...)` with private constructor
- All setters private; mutations via named methods (`Update`, `ChangeStatus`, `Delete`)
- Domain errors in sibling `*DomainErrors.cs` file

---

### Step 2 — Commands

Follow the **add-command** skill. Create three commands:

**Create command** (`src/ProjectName.Application/{Domain}/Commands/Create/`):
- `Create{AggregateName}Command.cs` — record + handler in same file
- `Create{AggregateName}CommandValidator.cs`

**Update command** (`src/ProjectName.Application/{Domain}/Commands/Update/`):
- `Update{AggregateName}Command.cs` — record + handler
- `Update{AggregateName}CommandValidator.cs`

**Delete command** (`src/ProjectName.Application/{Domain}/Commands/Delete/`):
- `Delete{AggregateName}Command.cs` — record + handler
- `Delete{AggregateName}CommandValidator.cs`

Also create `src/ProjectName.Application/{Domain}/{Domain}Errors.cs` with `{AggregateName}NotFound` error.

---

### Step 3 — Queries

Follow the **add-query** skill. Create:

**List query** (`src/ProjectName.Application/{Domain}/Queries/GetAll/`):
- `Get{AggregateName}ListQuery.cs` — query record + executor
- `{AggregateName}ListItemDto.cs`

**By-ID query** (`src/ProjectName.Application/{Domain}/Queries/GetById/`):
- `Get{AggregateName}ByIdQuery.cs` — query record + executor
- `{AggregateName}DetailsDto.cs`

**QueryReader** (`src/ProjectName.Application/{Domain}/Queries/`):
- `I{AggregateName}QueryReader.cs` — interface
- `src/ProjectName.Infrastructure/Queries/{AggregateName}QueryReader.cs` — Dapper implementation

---

### Step 4 — EF Core Configuration

Follow the **add-ef-configuration** skill.

File: `src/ProjectName.Infrastructure/EntityConfigurations/{AggregateName}Configuration.cs`

Key rules:
- All `string` columns need `.HasMaxLength()` matching aggregate constants
- Enums as `HasConversion<string>()` with `CK_{Table}_{Column}_Values` check constraint
- Timestamps as `"timestamp with time zone"`
- Index on `DeletedAt`

Register in `ApplicationDbContext`:
- `public DbSet<{AggregateName}> {AggregateName}s { get; set; }`

---

### Step 5 — Repository

Follow the **add-repository** skill.

File: `src/ProjectName.Infrastructure/Repositories/{AggregateName}Repository.cs`

- Extends `Repository<{AggregateName}, {KeyType}>`
- Implements `I{AggregateName}Repository`
- Register in `DependencyInjection.cs`: `services.AddScoped<I{AggregateName}Repository, {AggregateName}Repository>()`

---

### Step 6 — API Endpoints

Follow the **add-api-endpoint** skill.

File: `src/ProjectName.Api/Endpoints/{Domain}/{AggregateName}Endpoints.cs`

Routes to add:
| Method | Path | Auth | Idempotency |
|--------|------|------|-------------|
| GET | `/api/{domain}` | Administrator | No |
| GET | `/api/{domain}/{id}` | Administrator | No |
| POST | `/api/{domain}` | Administrator | Yes |
| PUT | `/api/{domain}/{id}` | Administrator | Yes |
| DELETE | `/api/{domain}/{id}` | Administrator | Yes |

Register `Map{AggregateName}Endpoints()` in `WebApplicationExtensions.InstallApplication()`.

---

### Step 7 — Unit Tests

Follow the **add-unit-tests** skill.

Files to create:
- `tests/ProjectName.UnitTests/Domain/{AggregateName}Aggregate/{AggregateName}Tests.cs`
- `tests/ProjectName.UnitTests/Application/{Domain}/Commands/Create{AggregateName}CommandHandlerTests.cs`
- `tests/ProjectName.UnitTests/Application/{Domain}/Commands/Update{AggregateName}CommandHandlerTests.cs`
- `tests/ProjectName.UnitTests/Application/{Domain}/Commands/Delete{AggregateName}CommandHandlerTests.cs`

---

### Step 8 — Migration Reminder

Remind the user to add an EF Core migration:

```bash
dotnet ef migrations add Add{AggregateName} --project src/ProjectName.Infrastructure --startup-project src/ProjectName.Api
```

Then verify the generated migration and run via Aspire (it auto-applies on startup).

---

## Reference Files to Read First

Before generating any code, read these files to understand existing patterns:

1. `src/ProjectName.Domain/AggregatesModel/PharmacyAggregate/Pharmacy.cs`
2. `src/ProjectName.Application/Pharmacies/Commands/Create/CreatePharmacyCommand.cs`
3. `src/ProjectName.Application/Pharmacies/Queries/GetAll/GetPharmacyListQueryExecutor.cs`
4. `src/ProjectName.Infrastructure/EntityConfigurations/PharmacyConfiguration.cs`
5. `src/ProjectName.Infrastructure/Repositories/PharmacyRepository.cs`
6. `src/ProjectName.Api/Endpoints/Pharmacies/PharmacyEndpoints.cs`

---

## Checklist Before Finishing

- [ ] All 7 steps completed in order
- [ ] Aggregate has private setters, `Create()` factory, `ValidateNotDeleted()` guards
- [ ] Commands use `SaveEntitiesAsync` (Create) and `SaveChangesAsync` (Update/Delete)
- [ ] Query executors apply `ApplySoftDeleteFilter` and use `SqlBuilder`
- [ ] `ApplicationDbContext` has the new `DbSet<>`
- [ ] Repository registered in `DependencyInjection.cs`
- [ ] Endpoints registered in `WebApplicationExtensions.InstallApplication()`
- [ ] All mutating endpoints have `.AddEndpointFilter<IdempotencyCheckerFilter>()`
- [ ] Unit tests cover all validation failure branches + success paths
- [ ] Migration command printed for the user
