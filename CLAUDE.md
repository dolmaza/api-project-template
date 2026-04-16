# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Overview

This is a `dotnet new` project template for an ASP.NET Core Web API following **Clean Architecture** principles, targeting **.NET 10**. The template generates a production-ready API scaffold. All source namespaces, file names, and folder names use `ProjectName` as a placeholder that gets replaced when `dotnet new api-template -n <name>` is run.

## Commands

```bash
# Install template locally
dotnet new install .\

# Verify installation
dotnet new list

# Scaffold a new project from the template
dotnet new api-template -n MyProject.API

# Build
dotnet build

# Run the API (from the template root — requires appsettings populated)
cd src/ProjectName.API && dotnet run

# Apply EF migrations manually (normally done automatically on startup)
dotnet ef database update --project src/ProjectName.Infrastructure --startup-project src/ProjectName.API
```

No test projects exist in this template; tests are added after scaffolding.

## Solution Structure

Two solution files exist:
- `ProjectName.slnx` — Core 4 projects (API, Application, Domain, Infrastructure)
- `ProjectName API.slnx` — All 5 projects including Azure

```
src/
├── ProjectName.API/              # Entry point: Minimal APIs, DI wiring, middleware
├── ProjectName.Application/      # Use-cases, service interfaces, validators, DTOs
├── ProjectName.Domain/           # Pure domain model, no framework dependencies
├── ProjectName.Infrastructure/   # EF Core, Identity, OpenIddict, Brevo, idempotency
└── ProjectName.Azure.Infrastructure/  # Azure Blob, Queue, OpenAI, MAF agents
```

## Architecture

### Layer Responsibilities

**Domain** — No framework dependencies. Contains:
- Custom mediator interfaces: `ICommand`, `IQuery`, `ICommandHandler`, `IQueryExecutor`, `IMediator`
- Result pattern: `Result<T>`, `Error`, `ErrorType` (Failure/Validation/NotFound/Conflict), `ValidationItem`
- DDD primitives: `Entity<TKey>`, `SoftDeletableEntity<TKey>`, `IAggregateRoot`, `IRepository<T,TId>`, `IUnitOfWork`
- Domain events: `IDomainEvent`, `IDomainEventHandler`, collected on entities via `HasDomainEvents`

**Application** — Use-case orchestration. Contains:
- Service interfaces: `IAccountService`, `IUserService`, `IMailService`, `IFileStorageService`, `IQueueService`
- FluentValidation validators (auto-registered and run by `ValidatorBehavior` in mediator pipeline)
- Dapper + SqlBuilder for read-model (query) side
- Constants for role names, policy names, queue names, regex patterns

**Infrastructure** — Implements application interfaces. Contains:
- `ApplicationDbContext` (EF Core + PostgreSQL via Npgsql)
- Mediator pipeline behaviors: `LoggingBehavior` → `ValidatorBehavior` → `TransactionBehaviour`
- ASP.NET Core Identity + OpenIddict OAuth 2.0 server (Password, Client Credentials, Auth Code, Refresh Token flows)
- Idempotency: `ClientRequest` entity + `RequestManager` — prevents duplicate command processing
- Generic `Repository<T, TId>` implementation
- `MailService` via Brevo (Sendinblue)

**API** — Minimal API endpoints organized by feature under `Endpoints/Identity/`. Uses:
- `IdempotencyCheckerFilter` on write endpoints
- `GlobalExceptionHandler` middleware
- `DatabaseInitializer` (auto-migrates + seeds on startup)
- Swagger UI served at `/swagger` in non-production

### Key Patterns

**Custom Mediator (no MediatR):** Handlers are auto-registered via assembly scanning. Pipeline behaviors execute in reverse registration order.

**Result pattern over exceptions:** All service methods return `Result<T>` or `Result`. Endpoints convert failures via `.ToProblemDetails()` extension. Never throw for expected business errors.

**Idempotency:** Write endpoints should apply `IdempotencyCheckerFilter`. Commands are deduplicated by correlation ID via `RequestManager`.

**Entity auditing:** `CreatedBy`, `CreatedAt`, `ModifiedBy`, `ModifiedAt` are auto-populated by the DbContext interceptor using `ICurrentStateService`. Use `SoftDeletableEntity<TKey>` for logical deletion.

**Domain events:** Add events to entities via `AddDomainEvent()`. They are published inside `SaveEntitiesAsync()` on `ApplicationDbContext` before/after the transaction commit.

## Configuration (appsettings)

Required sections in `appsettings.Development.json`:

| Key | Purpose |
|-----|---------|
| `ConnectionStrings:serche-med-db` | PostgreSQL connection string |
| `ConnectionStrings:blobs` | Azure Blob Storage (or Azurite) |
| `ConnectionStrings:queues` | Azure Queue Storage (or Azurite) |
| `IdentityConfig:Authority` | OpenIddict issuer URL |
| `IdentityConfig:EncryptionKey` | Base64-encoded 256-bit key |
| `SwaggerConfig:TokenUrl` | `/api/identity/connect/token` |
| `Cors:AllowOrigins` | Array of allowed frontend origins |
| `BrevoConfig` | Brevo API key and sender details |
| `Authentication:Google/Facebook` | Social OAuth credentials |
| `AzureAi:Endpoint/ApiKey` | Azure OpenAI resource |

## DI Registration Conventions

- Infrastructure services: `services.AddInfrastructure(configuration)`
- Azure services: `services.AddAzureInfrastructure(configuration)`
- Mediator command/query handlers: auto-registered by assembly scanning from Domain/Application assemblies
- FluentValidation validators: auto-registered from Application assembly
- Query executors: `services.AddAllQueryExecutors()`

## Startup Sequence

1. `DatabaseInitializer.InitializeAsync()` — applies pending EF migrations, seeds default data
2. `AzureStorageInitializer.InitializeAsync()` — creates Blob containers and Queues if absent
3. API begins serving requests; Swagger at `/swagger`, `/` redirects to `/swagger` (dev) or `/health` (prod)
