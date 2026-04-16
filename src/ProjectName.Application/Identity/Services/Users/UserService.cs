using System.Security.Claims;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using ProjectName.Application.Common.Abstractions;
using ProjectName.Application.Common.Extensions;
using ProjectName.Application.Identity.Services.Users.Dtos;
using ProjectName.Domain.AggregatesModel.IdentityAggregate;
using ProjectName.Domain.Common.Abstractions;
using ProjectName.Domain.Common.ResultPattern;

namespace ProjectName.Application.Identity.Services.Users;

public class UserService
    (
        UserManager<ApplicationUser> userManager, 
        ICurrentStateService currentStateService,
        ILogger<UserService> logger
    ) : IUserService
{
    ///<inheritdoc />
    public async Task<Result<AuthenticatedUserDto>> GetAuthenticatedUserAsync()
    {
        var userId = currentStateService.GetAuthorizedId();

        var user = await userManager.FindByIdAsync(userId);

        if (user is null)
        {
            return Result<AuthenticatedUserDto>.Failure(UserErrors.GetAuthenticatedUser.UserNotFound);
        }

        var userRoles = await userManager.GetRolesAsync(user);

        var userRolesArray = userRoles.Select(Enum.Parse<UserRole>).ToArray();

        var userDto = new AuthenticatedUserDto(user.Id, user.Email, user.Name, userRolesArray);

        return userDto;
    }

    ///<inheritdoc />
    public async Task<Result> ChangePasswordAsync(string currentPassword, string newPassword, CancellationToken cancellationToken)
    {
        var userId = currentStateService.GetAuthorizedId();

        var user = await userManager.FindByIdAsync(userId);

        if (user is null)
        {
            return UserErrors.ChangePassword.UserNotFound;
        }

        var result = await userManager.ChangePasswordAsync(user, currentPassword, newPassword);

        if (result.Succeeded)
        {
            return Result.Success();
        }
        else
        {
            logger.LogError("Couldn't change user password with id: {userId}, reason: {reason}", userId, result.ErrorsToString());

            return UserErrors.ChangePassword.FailedChangeUserPassword;
        }
    }

    ///<inheritdoc />
    public async Task<Result<string>> CreateUserAsync(UserDto userDto, CancellationToken cancellationToken)
    {
        var isUserExistsWithEmail = await IsUserExistsAsync(userDto.Email, cancellationToken);

        if (isUserExistsWithEmail)
        {
            return Result.Failure(UserErrors.CreateUser.UserExists);
        }

        var user = new ApplicationUser
        {
            Email = userDto.Email
        };

        var createUserResult = await userManager.CreateAsync(user);

        if (!createUserResult.Succeeded)
        {
            logger.LogError("Couldn't create user with email: {email}, reason: {reason}", userDto.Email, createUserResult.ErrorsToString());

            return Result.Failure(UserErrors.CreateUser.FailedSaveUser);
        }

        var role = userDto.Role.ToString();

        var addUserRoleResult = await userManager.AddToRoleAsync(user, role);

        if (!addUserRoleResult.Succeeded)
        {
            logger.LogError("Couldn't add user role for email: {email}, reason: {reason}", userDto.Email, addUserRoleResult.ErrorsToString());

            return Result.Failure(UserErrors.CreateUser.FailedAddRoleToUser);
        }

        var claims = new List<Claim>
        {
            new(ClaimTypes.Role, role)
        };

        var addClaimsResult = await userManager.AddClaimsAsync(user, claims);

        if (!addClaimsResult.Succeeded)
        {
            logger.LogError("Couldn't add user claims for email: {email}, reason: {reason}", userDto.Email, addClaimsResult.ErrorsToString());

            return Result.Failure(UserErrors.CreateUser.FailedAddClaimsToUser);
        }

        var addPasswordResult = await userManager.AddPasswordAsync(user, userDto.Password);

        if (!addPasswordResult.Succeeded)
        {
            logger.LogError("Couldn't add user password for email: {email}, reason: {reason}", userDto.Email, addPasswordResult.ErrorsToString());

            return Result.Failure(UserErrors.CreateUser.FailedAddPasswordToUser);
        }
        else
        {
            return Result<string>.Success(user.Id);
        }
    }

    ///<inheritdoc />
    public async Task<Result> UpdateUserAsync(UserUpdateDto userDto, CancellationToken cancellationToken)
    {
        var user = await userManager.FindByIdAsync(userDto.Id);

        if (user is null)
            return UserErrors.UpdateUser.UserNotFound;
        
        user.Email = userDto.Email;
        user.UserName = userDto.Email;
        user.Name = userDto.Name;

        var result = await userManager.UpdateAsync(user);

        if (result.Succeeded) 
            return Result.Success();
        
        logger.LogError("Couldn't update user for email: {email}, reason: {reason}", userDto.Email, result.ErrorsToString());

        return UserErrors.UpdateUser.FailedUpdateUser;
    }

    ///<inheritdoc />
    public async Task<Result> BlockUserAsync(string id, CancellationToken cancellationToken)
    {
        var user = await userManager.FindByIdAsync(id);

        if (user is null)
            return UserErrors.BlockOrActivateUser.UserNotFound;

        await userManager.SetLockoutEnabledAsync(user, true);
        await userManager.SetLockoutEndDateAsync(user, new DateTimeOffset(9999, 12, 31, 0, 0, 0, TimeSpan.Zero));

        return Result.Success();
    }

    ///<inheritdoc />
    public async Task<Result> ActivateUserAsync(string id, CancellationToken cancellationToken)
    {
        var user = await userManager.FindByIdAsync(id!);

        if (user is null)
            return UserErrors.BlockOrActivateUser.UserNotFound;

        await userManager.SetLockoutEnabledAsync(user, false);
        await userManager.SetLockoutEndDateAsync(user, null);

        return Result.Success();
    }

    ///<inheritdoc />
    public async Task<Result> UpdateProfileAsync(UserProfileUpdateDto userProfileDto, CancellationToken cancellationToken)
    {
        var userId = currentStateService.GetAuthorizedId();

        var user = await userManager.FindByIdAsync(userId);

        if (user is null)
            return UserErrors.UpdateProfile.UserNotFound;

        if (user.Email != userProfileDto.Email)
        {
            var emailExists = await IsUserExistsAsync(userProfileDto.Email, cancellationToken);
            if (emailExists)
                return UserErrors.UpdateProfile.EmailAlreadyExists;
        }

        user.Email = userProfileDto.Email;
        user.UserName = userProfileDto.Email;
        user.Name = userProfileDto.Name;

        var result = await userManager.UpdateAsync(user);

        if (result.Succeeded)
            return Result.Success();

        logger.LogError("Couldn't update user profile for userId: {userId}, reason: {reason}", userId, result.ErrorsToString());

        return UserErrors.UpdateProfile.FailedUpdateProfile;
    }

    private async Task<bool> IsUserExistsAsync(string? email, CancellationToken cancellationToken)
    {
        var normalizeEmail = userManager.NormalizeEmail(email);

        return await userManager.Users.AnyAsync(u => u.NormalizedEmail == normalizeEmail, cancellationToken);
    }
}