using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using ProjectName.API.Infrastructure.Filters;
using ProjectName.Application.Common.Abstractions;
using ProjectName.Application.Common.Constants;
using ProjectName.Application.Common.Extensions;
using ProjectName.Application.Identity.Models.Requests;
using ProjectName.Application.Identity.Services.Users.Dtos;

namespace ProjectName.Api.Endpoints.Identity;

public static class UserEndpoints
{
    public static IEndpointRouteBuilder MapUserEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/identity/users")
            .WithTags("Users")
            .RequireAuthorization();

        group.MapGet("/me", GetAuthenticatedUser)
            .WithName("GetAuthenticatedUser")
            .Produces<AuthenticatedUserDto>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status404NotFound);

        group.MapPut("/me", UpdateProfile)
            .WithName("UpdateProfile")
            .AddEndpointFilter<IdempotencyCheckerFilter>()
            .Produces(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);

        group.MapPost("/change-password", ChangePassword)
            .WithName("ChangePassword")
            .AddEndpointFilter<IdempotencyCheckerFilter>()
            .Produces(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status404NotFound);

        group.MapPost("/", CreateUser)
            .WithName("CreateUser")
            .AddEndpointFilter<IdempotencyCheckerFilter>()
            .RequireAuthorization(AuthorizationPolicyNames.AdministratorPolicy)
            .Produces(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status409Conflict);

        group.MapPut("/{id}", UpdateUser)
            .WithName("UpdateUser")
            .AddEndpointFilter<IdempotencyCheckerFilter>()
            .RequireAuthorization(AuthorizationPolicyNames.AdministratorPolicy)
            .Produces(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status404NotFound);

        group.MapPost("/{id}/block", BlockUser)
            .WithName("BlockUser")
            .RequireAuthorization(AuthorizationPolicyNames.AdministratorPolicy)
            .Produces(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status404NotFound);

        group.MapPost("/{id}/activate", ActivateUser)
            .WithName("ActivateUser")
            .AddEndpointFilter<IdempotencyCheckerFilter>()
            .RequireAuthorization(AuthorizationPolicyNames.AdministratorPolicy)
            .Produces(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status404NotFound);

        return app;
    }

    private static async Task<IResult> GetAuthenticatedUser(
        [FromServices] IUserService userService)
    {
        var result = await userService.GetAuthenticatedUserAsync();

        return result.IsSuccess
            ? Results.Ok(result.Value)
            : result.ToProblemDetails();
    }

    private static async Task<IResult> UpdateProfile(
        [FromBody] UpdateProfileRequest request,
        [FromServices] IUserService userService,
        [FromServices] IValidator<UpdateProfileRequest> validator,
        CancellationToken cancellationToken)
    {
        var validationResult = await validator.ValidateAsync(request, cancellationToken);
        if (!validationResult.IsValid)
        {
            return validationResult.ToResult().ToProblemDetails();
        }

        var userProfileDto = new UserProfileUpdateDto(request.Name, request.Email);
        var result = await userService.UpdateProfileAsync(userProfileDto, cancellationToken);

        return result.IsSuccess
            ? Results.Ok()
            : result.ToProblemDetails();
    }

    private static async Task<IResult> ChangePassword(
        [FromBody] ChangePasswordRequest request,
        [FromServices] IUserService userService,
        [FromServices] IValidator<ChangePasswordRequest> validator,
        CancellationToken cancellationToken)
    {
        var validationResult = await validator.ValidateAsync(request, cancellationToken);
        if (!validationResult.IsValid)
        {
            return validationResult.ToResult().ToProblemDetails();
        }

        var result = await userService.ChangePasswordAsync(request.CurrentPassword, request.NewPassword, cancellationToken);

        return result.IsSuccess
            ? Results.Ok()
            : result.ToProblemDetails();
    }

    private static async Task<IResult> CreateUser(
        [FromBody] CreateUserRequest request,
        [FromServices] IUserService userService,
        [FromServices] IValidator<CreateUserRequest> validator,
        CancellationToken cancellationToken)
    {
        var validationResult = await validator.ValidateAsync(request, cancellationToken);
        if (!validationResult.IsValid)
        {
            return validationResult.ToResult().ToProblemDetails();
        }

        var userDto = new UserDto(request.Email, request.Password, request.Role);
        var result = await userService.CreateUserAsync(userDto, cancellationToken);

        return result.IsSuccess
            ? Results.Ok(new { UserId = result.Value })
            : result.ToProblemDetails();
    }

    private static async Task<IResult> UpdateUser(
        [FromRoute] string id,
        [FromBody] UpdateUserRequest request,
        [FromServices] IUserService userService,
        [FromServices] IValidator<UpdateUserRequest> validator,
        CancellationToken cancellationToken)
    {
        if (id != request.Id)
        {
            return Results.BadRequest(new { Error = "Route ID does not match request ID" });
        }

        var validationResult = await validator.ValidateAsync(request, cancellationToken);
        if (!validationResult.IsValid)
        {
            return validationResult.ToResult().ToProblemDetails();
        }

        var userDto = new UserUpdateDto(request.Id, request.Name, request.Email);
        var result = await userService.UpdateUserAsync(userDto, cancellationToken);

        return result.IsSuccess
            ? Results.Ok()
            : result.ToProblemDetails();
    }

    private static async Task<IResult> BlockUser(
        [FromRoute] string id,
        [FromServices] IUserService userService,
        CancellationToken cancellationToken)
    {
        var result = await userService.BlockUserAsync(id, cancellationToken);

        return result.IsSuccess
            ? Results.Ok()
            : result.ToProblemDetails();
    }

    private static async Task<IResult> ActivateUser(
        [FromRoute] string id,
        [FromServices] IUserService userService,
        CancellationToken cancellationToken)
    {
        var result = await userService.ActivateUserAsync(id, cancellationToken);

        return result.IsSuccess
            ? Results.Ok()
            : result.ToProblemDetails();
    }
}