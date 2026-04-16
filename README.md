# Web API Template

A `dotnet new` project template for an ASP.NET Core Web API following Clean Architecture principles, targeting **.NET 10**.

## Features

- **Clean Architecture** — Domain, Application, Infrastructure, and API layers are clearly separated
- **Custom Mediator** — Lightweight, in-process mediator with pipeline behaviour support (no third-party dependency)
- **Result Pattern** — Discriminated `Result<T>` / `Error` types for explicit, exception-free error handling
- **OpenIddict** — OAuth 2.0 authorization server (Password flow, token endpoint, refresh tokens)
- **ASP.NET Core Identity** — User management, roles, email verification, password recovery, and TOTP support
- **Minimal APIs** — Endpoint-centric routing (`AccountEndpoints`, `AuthorizationEndpoints`, `UserEndpoints`)
- **Entity Framework Core + PostgreSQL** — Code-first data access via Npgsql provider
- **Dapper + SqlBuilder** — Lightweight read-model queries alongside EF Core
- **FluentValidation** — Request validation wired into the mediator pipeline
- **Idempotency** — `ClientRequest` / `RequestManager` pattern to safely replay commands
- **Serilog** — Structured logging with correlation-ID enrichment
- **Swagger / OpenAPI** — Auto-generated docs with OAuth2 Password-flow security definition
- **CORS** — Configurable allowed-origin list
- **SignalR** — Real-time communication support registered out of the box
- **Social Login** — Google and Facebook OAuth providers pre-wired
- **Azure Blob Storage** — File upload/download via `IFileStorageService`
- **Azure Queue Storage** — Message enqueue/dequeue via `IQueueService`
- **Azure AI / OpenAI** — `Azure.AI.OpenAI` + `Microsoft.Extensions.AI.OpenAI` integration
- **Microsoft Agents Framework (MAF)** — AI agent and workflow support via `Microsoft.Agents.AI`
- **Brevo (Sendinblue)** — Transactional email via `IMailService`
- **Domain-Driven Design primitives** — `Entity`, `IAggregateRoot`, `IRepository<T,TId>`, `IUnitOfWork`, domain events

## Solution structure

After scaffolding (see [Create a new project](#create-a-new-project)) the solution looks like this:

```
MyProject.API/
├── MyProject API.slnx
├── MyProject.slnx
└── src/
    ├── MyProject.API/                        # Entry point — Minimal API endpoints, middleware, DI wiring
    │   ├── Endpoints/Identity/               # Account, Authorization, User endpoints
    │   ├── Infrastructure/
    │   │   ├── Extensions/                   # Builder / WebApplication extension methods
    │   │   ├── Filters/                      # IdempotencyCheckerFilter
    │   │   └── Middleware/                   # GlobalExceptionHandler
    │   ├── Program.cs
    │   ├── appsettings.json
    │   └── appsettings.Development.json
    │
    ├── MyProject.Application/                # Use-cases, service interfaces, validators, DTOs
    │   ├── Common/Abstractions/              # IAccountService, IUserService, IMailService, IFileStorageService, IQueueService …
    │   ├── Common/Constants/                 # Role names, policy names, queue names, regex patterns …
    │   ├── Common/Extensions/               # Identity, FluentValidation, Dapper SqlBuilder helpers
    │   ├── Common/Models/                    # QueryListResult, QueryPagedListResult
    │   └── Identity/                         # Account & User services, request models, validators
    │
    ├── MyProject.Domain/                     # Pure domain model — no framework dependencies
    │   ├── AggregatesModel/IdentityAggregate/ # ApplicationUser, UserRole
    │   ├── Common/Abstractions/              # ICommand, IQuery, ICommandHandler, IQueryExecutor …
    │   ├── Common/DomainEvents/              # IDomainEvent, IDomainEventHandler
    │   ├── Common/Mediator/                  # Custom IMediator, IPipelineBehavior, Mediator implementation
    │   ├── Common/ResultPattern/             # Result<T>, Error, ErrorType, ValidationItem
    │   └── SeedWork/                         # Entity, IAggregateRoot, IRepository, IUnitOfWork
    │
    ├── MyProject.Infrastructure/             # EF Core, Identity, Brevo mail, idempotency, mediator behaviours
    │   ├── Behaviours/                       # LoggingBehaviour, TransactionBehaviour, ValidatorBehavior
    │   ├── Database/                         # ApplicationDbContext, entity configurations, DatabaseInitializer
    │   ├── Idempotency/                      # ClientRequest, RequestManager
    │   ├── Repositories/                     # Generic Repository<T, TId>
    │   └── Services/                         # MailService (Brevo)
    │
    └── MyProject.Azure.Infrastructure/       # Azure Storage, Azure AI, MAF agents
        ├── Configs/                          # AzureStorageConfig, AzureAiSettings, PrescriptionAgentSettings
        ├── Services/                         # AzureBlobStorageService, AzureQueueService
        └── AzureStorageInitializer.cs
```

## Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download) or later
- A running **PostgreSQL** instance
- An **Azure Storage** account (Blob + Queue) — or [Azurite](https://learn.microsoft.com/en-us/azure/storage/common/storage-use-azurite) for local development
- *(Optional)* Azure OpenAI resource for AI features

## Install the template

From the root of this repository (the folder containing `.template.config`), run:

```bash
dotnet new install .\
```

Verify the template is registered:

```bash
dotnet new list
```

You should see `Web API Template` with short name `api-template` in the list.

## Create a new project

```bash
dotnet new api-template -n MyProject.API
```

Every occurrence of `ProjectName` in file names, folder names, and source code is replaced by `MyProject`.

## Configuration

After scaffolding, populate `appsettings.Development.json` (or user secrets / environment variables) with the following sections:

```json
{
  "ConnectionStrings": {
    "serche-med-db": "<PostgreSQL connection string>",
    "blobs": "<Azure Blob Storage connection string>",
    "queues": "<Azure Queue Storage connection string>"
  },
  "IdentityConfig": {
    "Authority": "https://localhost:<port>",
    "ClaimsIssuer": "https://localhost:<port>",
    "EncryptionKey": "<base64-encoded 256-bit key>"
  },
  "SwaggerConfig": {
    "TokenUrl": "/api/identity/connect/token",
    "ClientId": "<swagger-oauth-client-id>",
    "ClientSecret": "<swagger-oauth-client-secret>"
  },
  "Cors": {
    "AllowOrigins": [ "http://localhost:8080" ]
  },
  "BrevoConfig": {
    "Url": "https://api.brevo.com",
    "ApiKey": "<brevo-api-key>",
    "FromEmail": "noreply@example.com",
    "FromEmailName": "My Project"
  },
  "Authentication": {
    "Google": { "ClientId": "", "ClientSecret": "" },
    "Facebook": { "AppId": "", "AppSecret": "" }
  },
  "AzureAi": {
    "Endpoint": "<azure-openai-endpoint>",
    "ApiKey": "<azure-openai-api-key>"
  }
}
```

## Running the API

```bash
cd src/MyProject.API
dotnet run
```

The application will:

1. Apply any pending EF Core migrations and seed the database (`DatabaseInitializer`)
2. Create required Azure Blob containers and Queues (`AzureStorageInitializer`)
3. Serve Swagger UI at `/swagger` (non-production environments)
4. Redirect `/` to `/swagger` (dev) or `/health` (production)

## Key packages

| Layer | Package | Purpose |
|---|---|---|
| API | `OpenIddict.AspNetCore` | OAuth 2.0 / OIDC server |
| API | `Microsoft.AspNetCore.Authentication.JwtBearer` | JWT bearer validation |
| API | `Serilog.AspNetCore` | Structured logging |
| API | `Swashbuckle.AspNetCore` | Swagger / OpenAPI docs |
| Application | `FluentValidation` | Request validation |
| Application | `Dapper` + `Dapper.SqlBuilder` | Read-model queries |
| Infrastructure | `Npgsql.EntityFrameworkCore.PostgreSQL` | PostgreSQL provider |
| Infrastructure | `OpenIddict.EntityFrameworkCore` | OpenIddict EF Core stores |
| Infrastructure | `brevo_csharp` | Transactional email |
| Azure.Infrastructure | `Azure.Storage.Blobs` | Blob storage |
| Azure.Infrastructure | `Azure.Storage.Queues` | Queue storage |
| Azure.Infrastructure | `Azure.AI.OpenAI` | Azure OpenAI client |
| Azure.Infrastructure | `Microsoft.Agents.AI` | MAF agent runtime |

## Uninstall the template

From the root of this repository, run:

```bash
dotnet new uninstall .\
```
