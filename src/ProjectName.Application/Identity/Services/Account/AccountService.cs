using System.Security.Claims;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using ProjectName.Application.Common.Abstractions;
using ProjectName.Application.Common.Constants;
using ProjectName.Application.Common.Extensions;
using ProjectName.Application.Common.Providers;
using ProjectName.Application.Identity.Services.Account.Dtos;
using ProjectName.Domain.AggregatesModel.IdentityAggregate;
using ProjectName.Domain.Common.ResultPattern;

namespace ProjectName.Application.Identity.Services.Account;

public class AccountService
(
    UserManager<ApplicationUser> userManager,
    IMailService mailService,
    ILogger<AccountService> logger
) : IAccountService
{
    ///<inheritdoc />
    public async Task<Result<RegisterUserResultDto>> RegisterUserAsync(RegisterUserDto registerUserDto, CancellationToken cancellationToken)
    {
        var isUserExistsWithEmail = await IsUserExistsAsync(registerUserDto.Email, cancellationToken);

        if (isUserExistsWithEmail)
        {
            return Result.Failure(AccountErrors.RegisterUser.UserExists);
        }

        var user = new ApplicationUser
        {
            Email = registerUserDto.Email,
            Name = registerUserDto.Name,
            UserName = registerUserDto.Email
        };

        var createUserResult = await userManager.CreateAsync(user);

        if (!createUserResult.Succeeded)
        {
            logger.LogError("Couldn't create user with email: {email}, reason: {reason}", registerUserDto.Email, createUserResult.ErrorsToString());

            return Result.Failure(AccountErrors.RegisterUser.FailedSaveUser);
        }

        const UserRole role = UserRole.Customer;

        var addUserRoleResult = await userManager.AddToRoleAsync(user, role.ToString());

        if (!addUserRoleResult.Succeeded)
        {
            logger.LogError("Couldn't add user role for email: {email}, reason: {reason}", registerUserDto.Email, addUserRoleResult.ErrorsToString());

            return Result.Failure(AccountErrors.RegisterUser.FailedAddRoleToUser);
        }

        var claims = new List<Claim>
        {
            new(ClaimTypes.Role, role.ToString())
        };

        var addClaimsResult = await userManager.AddClaimsAsync(user, claims);

        if (!addClaimsResult.Succeeded)
        {
            logger.LogError("Couldn't add user claims for email: {email}, reason: {reason}", registerUserDto.Email, addClaimsResult.ErrorsToString());

            return Result.Failure(AccountErrors.RegisterUser.FailedAddClaimsToUser);
        }

        var addPasswordResult = await userManager.AddPasswordAsync(user, registerUserDto.Password);

        if (!addPasswordResult.Succeeded)
        {
            logger.LogError("Couldn't add user password for email: {email}, reason: {reason}", registerUserDto.Email, addPasswordResult.ErrorsToString());

            return Result.Failure(AccountErrors.RegisterUser.FailedAddPasswordToUser);
        }

        var sendVerificationCodeResult = await SendVerificationCodeToUserEmailAsync(user, VerificationTokenPurposes.VerifyOwner);

        if (sendVerificationCodeResult.IsSuccess)
        {
            return Result<RegisterUserResultDto>.Success(new RegisterUserResultDto(user.Id, user.UserName));
        }
        else
        {
            logger.LogError("Couldn't send verification code for email: {email}", registerUserDto.Email);

            return sendVerificationCodeResult;
        }
    }

    ///<inheritdoc />
    public async Task<Result> VerifyRegistrationTokenAsync(string userName, string otp, CancellationToken cancellationToken)
    {
        var user = await userManager.FindByNameAsync(userName);

        if (user is null)
        {
            return AccountErrors.VerifyRegistrationToken.UserNotFound;
        }

        if (await userManager.IsEmailConfirmedAsync(user) || await userManager.IsPhoneNumberConfirmedAsync(user))
        {
            return AccountErrors.VerifyRegistrationToken.UserAlreadyVerified;
        }

        var isTokenValid = await userManager.VerifyUserTokenAsync(user, nameof(TotpTokenProvider<ApplicationUser>), VerificationTokenPurposes.VerifyOwner, otp);

        if (!isTokenValid)
        {
            return AccountErrors.VerifyRegistrationToken.WrongOtp;
        }

        user.EmailConfirmed = !string.IsNullOrEmpty(user.Email);

        var updateUserResult = await userManager.UpdateAsync(user);

        if (!updateUserResult.Succeeded)
        {
            return AccountErrors.VerifyRegistrationToken.FailedUserUpdate;
        }

        var claims = new[]
        {
            new Claim("email_verified", $"{user.EmailConfirmed}")
        };

        var addClaimsResult = await userManager.AddClaimsAsync(user, claims);

        if (!addClaimsResult.Succeeded)
        {
            return AccountErrors.VerifyRegistrationToken.FailedAddClaimsToUser;
        }
       
        return Result.Success();
    }

    ///<inheritdoc />
    public async Task<Result> ResendVerificationTokenAsync(string userName, string purpose, CancellationToken cancellationToken)
    {
        var user = await userManager.FindByNameAsync(userName);

        if (user is null)
        {
            return AccountErrors.ResendVerifyRegistration.UserNotFound;
        }

        if (await userManager.IsEmailConfirmedAsync(user) || await userManager.IsPhoneNumberConfirmedAsync(user))
        {
            return AccountErrors.ResendVerifyRegistration.UserAlreadyVerified;
        }

        return await SendVerificationCodeToUserEmailAsync(user, purpose);
    }

    ///<inheritdoc />
    public async Task<Result> SendPasswordRecoveryCodeAsync(string email, CancellationToken cancellationToken)
    {
        var user = await userManager.FindByEmailAsync(email);

        if (user is not { EmailConfirmed: true })
        {
            return AccountErrors.SendPasswordRecoveryCode.UserNotFound;
        }

        return await SendVerificationCodeToUserEmailAsync(user, VerificationTokenPurposes.PasswordRecovery);
    }

    ///<inheritdoc />
    public async Task<Result<string>> VerifyPasswordRecoveryOtpAsync(string email, string otp, CancellationToken cancellationToken)
    {
        var user = await userManager.FindByEmailAsync(email);

        if (user is null)
        {
            return Result.Failure(AccountErrors.VerifyPasswordRecoveryOtp.UserNotFound);
        }

        var isOtpValid = await userManager.VerifyUserTokenAsync(user, nameof(TotpTokenProvider<ApplicationUser>), VerificationTokenPurposes.PasswordRecovery, otp);

        if (!isOtpValid)
        {
            return Result.Failure(AccountErrors.VerifyPasswordRecoveryOtp.WrongOtp);
        }

        var passwordResetToken = await userManager.GeneratePasswordResetTokenAsync(user);

        return passwordResetToken;
    }

    ///<inheritdoc />
    public async Task<Result> PasswordRecoveryAsync(string token, string email, string password, CancellationToken cancellationToken)
    {
        var user = await userManager.FindByEmailAsync(email);

        if (user is null)
        {
            logger.LogWarning("Password recovery attempted for non-existent user with email: {Email}", email);
            return AccountErrors.PasswordRecovery.UserNotFound;
        }

        logger.LogInformation("Attempting password recovery for user {Email}", email);

        // Validate the token first
        var isTokenValid = await userManager.VerifyUserTokenAsync(user, TokenOptions.DefaultProvider, UserManager<ApplicationUser>.ResetPasswordTokenPurpose, token);

        if (!isTokenValid)
        {
            logger.LogWarning("Invalid or expired password reset token for user {Email}", email);
            return AccountErrors.PasswordRecovery.InvalidToken;
        }

        // Check if password meets the policy requirements before attempting reset
        var passwordValidationResult = await ValidatePasswordAsync(user, password);

        if (!passwordValidationResult.Succeeded)
        {
            var passwordErrors = string.Join(", ", passwordValidationResult.Errors.Select(e => $"{e.Code}: {e.Description}"));
            logger.LogWarning("Password policy validation failed for user {Email}. Errors: {Errors}", email, passwordErrors);
            return AccountErrors.PasswordRecovery.PasswordPolicyViolation;
        }

        // Attempt to reset the password
        var result = await userManager.ResetPasswordAsync(user, token, password);

        if (!result.Succeeded)
        {
            // Log the specific Identity errors for debugging
            var errors = string.Join(", ", result.Errors.Select(e => $"{e.Code}: {e.Description}"));
            logger.LogError("Password reset failed for user {Email}. Errors: {Errors}", email, errors);
            
            // Check for specific error types
            var identityErrors = result.Errors.ToList();
            
            if (identityErrors.Any(e => e.Code.Contains("InvalidToken") || e.Code.Contains("Token")))
            {
                return AccountErrors.PasswordRecovery.InvalidToken;
            }
            
            return identityErrors.Any(e => e.Code.Contains("Password")) 
                ? AccountErrors.PasswordRecovery.PasswordPolicyViolation 
                : AccountErrors.PasswordRecovery.FailedResetPassword;
        }

        logger.LogInformation("Password successfully reset for user {Email}", email);
        
        // Optionally, you can update the user's security stamp to invalidate existing tokens
        await userManager.UpdateSecurityStampAsync(user);
        
        return Result.Success();
    }

    private async Task<IdentityResult> ValidatePasswordAsync(ApplicationUser user, string password)
    {
        var passwordValidators = userManager.PasswordValidators;
        var errors = new List<IdentityError>();

        foreach (var validator in passwordValidators)
        {
            var result = await validator.ValidateAsync(userManager, user, password);
            if (!result.Succeeded)
            {
                errors.AddRange(result.Errors);
            }
        }

        return errors.Any() ? IdentityResult.Failed(errors.ToArray()) : IdentityResult.Success;
    }

    private async Task<bool> IsUserExistsAsync(string? email, CancellationToken cancellationToken)
    {
        var normalizeEmail = userManager.NormalizeEmail(email);

        return await userManager.Users.AnyAsync(u => u.NormalizedEmail == normalizeEmail, cancellationToken);
    }

    private async Task<Result> SendVerificationCodeToUserEmailAsync(ApplicationUser user, string purpose)
    {
        var token = await userManager.GenerateUserTokenAsync(user, nameof(TotpTokenProvider<ApplicationUser>), purpose);

        if (string.IsNullOrEmpty(user.Email))
        {
            return AccountErrors.SendVerificationCodeFailed;
        }

        await mailService.SendMailBrevoAsync(user.Email, "Verification Code", $"Code: {token}");

        return Result.Success();
    }
}