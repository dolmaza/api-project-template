using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using ProjectName.API.Infrastructure.Filters;
using ProjectName.Application.Common.Abstractions;
using ProjectName.Application.Common.Constants;
using ProjectName.Application.Common.Extensions;
using ProjectName.Application.Identity.Models.Requests;
using ProjectName.Application.Identity.Services.Account.Dtos;

namespace ProjectName.API.Endpoints.Identity;

public static class AccountEndpoints
{
    public static IEndpointRouteBuilder MapAccountEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/identity/accounts")
            .WithTags("Identity")
            .AllowAnonymous();

        group.MapPost("/register", RegisterUser)
            .WithName(nameof(RegisterUser))
            .AddEndpointFilter<IdempotencyCheckerFilter>()
            .Produces<RegisterUserResultDto>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status400BadRequest);

        group.MapPost("/verify-registration", VerifyRegistration)
            .WithName("VerifyRegistrationToken")
            .AddEndpointFilter<IdempotencyCheckerFilter>()
            .Produces(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status400BadRequest);

        group.MapPost("/verify-registration/resend", ResendRegistrationVerificationToken)
            .WithName(nameof(ResendRegistrationVerificationToken))
            .AddEndpointFilter<IdempotencyCheckerFilter>()
            .Produces(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status400BadRequest);

        group.MapPost("/password-recovery/send-code", SendPasswordRecoveryCode)
            .WithName(nameof(SendPasswordRecoveryCode))
            .AddEndpointFilter<IdempotencyCheckerFilter>()
            .Produces(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status400BadRequest);

        group.MapPost("/password-recovery/verify-otp", VerifyPasswordRecoveryOtp)
            .WithName(nameof(VerifyPasswordRecoveryOtp))
            .AddEndpointFilter<IdempotencyCheckerFilter>()
            .Produces(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status400BadRequest);

        group.MapPost("/password-recovery/reset", ResetPassword)
            .WithName(nameof(ResetPassword))
            .AddEndpointFilter<IdempotencyCheckerFilter>()
            .Produces(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status400BadRequest);

        return app;
    }

    private static async Task<IResult> RegisterUser(
        [FromBody] RegisterUserRequest request,
        [FromServices] IAccountService accountService,
        [FromServices] IValidator<RegisterUserRequest> validator,
        CancellationToken cancellationToken)
    {
        var validationResult = await validator.ValidateAsync(request, cancellationToken);
        if (!validationResult.IsValid)
        {
            return validationResult.ToResult().ToProblemDetails();
        }

        var registerUserDto = new RegisterUserDto(request.Email, request.Name, request.Password);
        var result = await accountService.RegisterUserAsync(registerUserDto, cancellationToken);

        return result.IsSuccess
            ? Results.Ok(result.Value)
            : result.ToProblemDetails();
    }

    private static async Task<IResult> VerifyRegistration(
        [FromBody] VerifyRegistrationTokenRequest request,
        [FromServices] IAccountService accountService,
        [FromServices] IValidator<VerifyRegistrationTokenRequest> validator,
        CancellationToken cancellationToken)
    {
        var validationResult = await validator.ValidateAsync(request, cancellationToken);
        if (!validationResult.IsValid)
        {
            return validationResult.ToResult().ToProblemDetails();
        }

        var result = await accountService.VerifyRegistrationTokenAsync(request.Email, request.Otp, cancellationToken);

        return result.IsSuccess
            ? Results.Ok()
            : result.ToProblemDetails();
    }

    private static async Task<IResult> ResendRegistrationVerificationToken(
        [FromBody] ResendVerificationTokenRequest request,
        [FromServices] IAccountService accountService,
        [FromServices] IValidator<ResendVerificationTokenRequest> validator,
        CancellationToken cancellationToken)
    {
        var validationResult = await validator.ValidateAsync(request, cancellationToken);
        if (!validationResult.IsValid)
        {
            return validationResult.ToResult().ToProblemDetails();
        }

        var result = await accountService.ResendVerificationTokenAsync(request.Email, VerificationTokenPurposes.VerifyOwner, cancellationToken);

        return result.IsSuccess
            ? Results.Ok()
            : result.ToProblemDetails();
    }

    private static async Task<IResult> SendPasswordRecoveryCode(
        [FromBody] SendPasswordRecoveryCodeRequest request,
        [FromServices] IAccountService accountService,
        [FromServices] IValidator<SendPasswordRecoveryCodeRequest> validator,
        CancellationToken cancellationToken)
    {
        var validationResult = await validator.ValidateAsync(request, cancellationToken);
        if (!validationResult.IsValid)
        {
            return validationResult.ToResult().ToProblemDetails();
        }

        var result = await accountService.SendPasswordRecoveryCodeAsync(request.Email, cancellationToken);

        return result.IsSuccess
            ? Results.Ok()
            : result.ToProblemDetails();
    }

    private static async Task<IResult> VerifyPasswordRecoveryOtp(
        [FromBody] VerifyPasswordRecoveryOtpRequest request,
        [FromServices] IAccountService accountService,
        [FromServices] IValidator<VerifyPasswordRecoveryOtpRequest> validator,
        CancellationToken cancellationToken)
    {
        var validationResult = await validator.ValidateAsync(request, cancellationToken);
        if (!validationResult.IsValid)
        {
            return validationResult.ToResult().ToProblemDetails();
        }

        var result = await accountService.VerifyPasswordRecoveryOtpAsync(request.Email, request.Otp, cancellationToken);

        return result.IsSuccess
            ? Results.Ok(new { Token = result.Value })
            : result.ToProblemDetails();
    }

    private static async Task<IResult> ResetPassword(
        [FromBody] PasswordRecoveryRequest request,
        [FromServices] IAccountService accountService,
        [FromServices] IValidator<PasswordRecoveryRequest> validator,
        CancellationToken cancellationToken)
    {
        var validationResult = await validator.ValidateAsync(request, cancellationToken);
        if (!validationResult.IsValid)
        {
            return validationResult.ToResult().ToProblemDetails();
        }

        var result = await accountService.PasswordRecoveryAsync(request.Token, request.Email, request.Password, cancellationToken);

        return result.IsSuccess
            ? Results.Ok()
            : result.ToProblemDetails();
    }
}