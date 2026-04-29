---
name: add-api-endpoint
description: Scaffold a Minimal API endpoint class for an existing aggregate — endpoint group(s), route handlers for commands (via IMediator) and queries (via executor), request body DTOs, idempotency filter on mutating routes, and registration in WebApplicationExtensions. Use when the user asks to expose an aggregate's commands/queries over HTTP.
allowed-tools: Read Write Glob Grep
---

You are an expert in this codebase's Minimal API patterns. Given an aggregate name and its operations, generate the endpoint file, request DTOs, and register the endpoints in `WebApplicationExtensions`.

## User's Request

$ARGUMENTS

---

## What to Generate

1. `src/ProjectName.Api/Endpoints/{Domain}/{Domain}Endpoints.cs` — endpoint class with route handlers
2. `src/ProjectName.Api/Endpoints/{Domain}/Requests/` — one request record per mutating operation
3. Update `src/ProjectName.Api/Infrastructure/Extensions/WebApplicationExtensions.cs` — add `.Map{Domain}Endpoints()` call

---

## Endpoint Class Template

```csharp
using Microsoft.AspNetCore.Mvc;
using ProjectName.Api.Endpoints.{Domain}.Requests;
using ProjectName.Api.Infrastructure.Filters;
using ProjectName.Application.Common.Constants;
using ProjectName.Application.Common.Extensions;
using ProjectName.Application.Common.Models;
using ProjectName.Application.{Domain}.Commands.Create;
using ProjectName.Application.{Domain}.Commands.Delete;
using ProjectName.Application.{Domain}.Commands.Update;
using ProjectName.Application.{Domain}.Queries.Dtos;
using ProjectName.Application.{Domain}.Queries.Get{AggregateName}s;
using ProjectName.Application.{Domain}.Queries.Get{AggregateName}ById;
using ProjectName.Domain.Common.Mediator.Abstractions;

namespace ProjectName.Api.Endpoints.{Domain};

public static class {Domain}Endpoints
{
    public static IEndpointRouteBuilder Map{Domain}Endpoints(this IEndpointRouteBuilder app)
    {
        // Protected group (admin-only example — adjust policy to CustomerPolicy if needed)
        var group = app.MapGroup("/api/{domain-kebab}")
            .WithTags("{Domain}")
            .RequireAuthorization(AuthorizationPolicyNames.AdministratorPolicy);

        // Optionally add a public group for unauthenticated endpoints:
        // var publicGroup = app.MapGroup("/api/{domain-kebab}")
        //     .WithTags("{Domain}")
        //     .AllowAnonymous();

        group.MapGet("/", Get{AggregateName}s)
            .WithName(nameof(Get{AggregateName}s))
            .Produces<QueryPagedListResult<{AggregateName}ListDto>>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status400BadRequest);

        group.MapGet("{id}", Get{AggregateName}ById)
            .WithName(nameof(Get{AggregateName}ById))
            .Produces<{AggregateName}Dto>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status404NotFound);

        group.MapPost("/", Create{AggregateName})
            .WithName(nameof(Create{AggregateName}))
            .AddEndpointFilter<IdempotencyCheckerFilter>()
            .Produces<{KeyType}>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status409Conflict);

        group.MapPut("/", Update{AggregateName})
            .WithName(nameof(Update{AggregateName}))
            .AddEndpointFilter<IdempotencyCheckerFilter>()
            .Produces(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);

        group.MapDelete("{id}", Delete{AggregateName})
            .WithName(nameof(Delete{AggregateName}))
            .AddEndpointFilter<IdempotencyCheckerFilter>()
            .Produces(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);

        return app;
    }

    // ── Query handlers — inject executor directly, bypass mediator ──────────

    private static async Task<IResult> Get{AggregateName}s(
        [FromQuery] string? searchValue,
        [FromQuery] int skip,
        [FromQuery] int take,
        [FromServices] Get{AggregateName}sQueryExecutor executor,
        CancellationToken cancellationToken)
    {
        var result = await executor.ExecuteAsync(
            new Get{AggregateName}sQuery(searchValue, skip, take), cancellationToken);
        return result.IsSuccess ? Results.Ok(result.Value) : result.ToProblemDetails();
    }

    private static async Task<IResult> Get{AggregateName}ById(
        [FromRoute] {KeyType} id,
        [FromServices] Get{AggregateName}ByIdQueryExecutor executor,
        CancellationToken cancellationToken)
    {
        var result = await executor.ExecuteAsync(new Get{AggregateName}ByIdQuery(id), cancellationToken);
        return result.IsSuccess ? Results.Ok(result.Value) : result.ToProblemDetails();
    }

    // ── Command handlers — route through IMediator ───────────────────────────

    private static async Task<IResult> Create{AggregateName}(
        [FromBody] Create{AggregateName}Request request,
        [FromServices] IMediator mediator,
        CancellationToken cancellationToken)
    {
        var command = new Create{AggregateName}Command(request.Name /*, map all fields */);
        var result = await mediator.Send(command, cancellationToken);
        return result.IsSuccess ? Results.Ok(result.Value) : result.ToProblemDetails();
    }

    private static async Task<IResult> Update{AggregateName}(
        [FromBody] Update{AggregateName}Request request,
        [FromServices] IMediator mediator,
        CancellationToken cancellationToken)
    {
        var command = new Update{AggregateName}Command(request.Id, request.Name /*, map all fields */);
        var result = await mediator.Send(command, cancellationToken);
        return result.IsSuccess ? Results.Ok() : result.ToProblemDetails();
    }

    private static async Task<IResult> Delete{AggregateName}(
        [FromRoute] {KeyType} id,
        [FromServices] IMediator mediator,
        CancellationToken cancellationToken)
    {
        var command = new Delete{AggregateName}Command(id);
        var result = await mediator.Send(command, cancellationToken);
        return result.IsSuccess ? Results.Ok() : result.ToProblemDetails();
    }
}
```

---

## Request DTOs Template

```csharp
// Requests/Create{AggregateName}Request.cs
namespace ProjectName.Api.Endpoints.{Domain}.Requests;

public record Create{AggregateName}Request(string Name /*, other fields */);
```

```csharp
// Requests/Update{AggregateName}Request.cs
namespace ProjectName.Api.Endpoints.{Domain}.Requests;

public record Update{AggregateName}Request({KeyType} Id, string Name /*, other fields */);
```

No request DTO is needed for Delete (the id comes from the route).

---

## Registration in WebApplicationExtensions

Open `src/ProjectName.Api/Infrastructure/Extensions/WebApplicationExtensions.cs` and append the new call to the chain:

```csharp
app.MapIdentityEndpoints()
   // ... existing entries ...
   .Map{Domain}Endpoints();   // ← add this line
```

---

## Authorization Policy Reference

| Policy constant | Who can access |
|---|---|
| `AuthorizationPolicyNames.AdministratorPolicy` | Admin users only |
| `AuthorizationPolicyNames.CustomerPolicy` | Authenticated customers |
| `.AllowAnonymous()` | Public — no auth required |

Use separate `MapGroup` calls on the same path prefix when some routes need different auth than others.

## Route Conventions

| Operation | Method | Path |
|---|---|---|
| List / paged | `GET` | `/api/{domain}` |
| By-id | `GET` | `/api/{domain}/{id}` |
| Create | `POST` | `/api/{domain}` |
| Update (full replace) | `PUT` | `/api/{domain}` (id in body) |
| Delete | `DELETE` | `/api/{domain}/{id}` |

Use `:int` or `:long` route constraint when the key type is numeric (e.g., `{id:int}`).

## Important Rules

- **Always** add `IdempotencyCheckerFilter` to `POST`, `PUT`, and `DELETE` routes
- **Queries** use `[FromServices] {Executor}` — never `IMediator` for reads
- **Commands** use `[FromServices] IMediator` — never the executor directly
- Every route must declare `.Produces<T>()` and `.ProducesProblem()` for Swagger accuracy
- Route handler methods are `private static async Task<IResult>`
- Request DTOs are `record` types in the `Requests/` sub-folder
- Namespace: `ProjectName.Api.Endpoints.{Domain}`

## Checklist Before Finishing

- [ ] Endpoint class is `public static class {Domain}Endpoints`
- [ ] Extension method is `Map{Domain}Endpoints(this IEndpointRouteBuilder app)`
- [ ] GET routes use executor injection; POST/PUT/DELETE use `IMediator`
- [ ] All mutating routes have `.AddEndpointFilter<IdempotencyCheckerFilter>()`
- [ ] All routes declare `.Produces<T>()` and `.ProducesProblem()` responses
- [ ] Request records are in `Endpoints/{Domain}/Requests/` folder
- [ ] `WebApplicationExtensions.cs` is updated with `.Map{Domain}Endpoints()`
- [ ] Correct route constraints used for typed IDs (e.g., `{id:int}`)
