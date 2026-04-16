using ProjectName.Domain.Common.ResultPattern;

namespace ProjectName.Application.Identity.Services.Users;

public static class UserErrors
{
    public static class GetAuthenticatedUser
    {
        public static Error UserNotFound => Error.NotFound("GetAuthenticatedUser.UserNotFound", "Couldn't find user");
    }

    public static class ChangePassword
    {
        public static Error UserNotFound => Error.NotFound("ChangePassword.UserNotFound", "Couldn't find user");
        public static Error FailedChangeUserPassword => Error.NotFound("ChangePassword.FailedChangeUserPassword", "Couldn't change user password");
    }

    public static class CreateUser
    {
        public static Error UserExists => Error.Conflict("CreateUser.UserExists", $"User exists with provided Email or UserName");

        public static Error FailedSaveUser => Error.Failure("CreateUser.FailedSaveUser", "Error while saving user in database");

        public static Error FailedAddRoleToUser => Error.Failure("CreateUser.FailedAddRoleToUser", "Error while saving user role in database");

        public static Error FailedAddClaimsToUser => Error.Failure("CreateUser.FailedAddClaimsToUser", "Error while saving user claims in database");

        public static Error FailedAddPasswordToUser => Error.Failure("CreateUser.FailedAddPasswordToUser", "Error while saving user password in database");

    }

    public static class UpdateUser
    {
        public static Error UserNotFound => Error.NotFound("UpdateUser.UserNotFound", "Couldn't find user");
        public static Error FailedUpdateUser => Error.Failure("UpdateUser.FailedUpdateUser", "Error while saving user in database");

    }

    public static class BlockOrActivateUser
    {
        public static Error UserNotFound => Error.NotFound("BlockOrActivateUser.UserNotFound", "Couldn't find user");
    }

    public static class UpdateProfile
    {
        public static Error UserNotFound => Error.NotFound("UpdateProfile.UserNotFound", "Couldn't find user");
        public static Error FailedUpdateProfile => Error.Failure("UpdateProfile.FailedUpdateProfile", "Error while updating user profile");
        public static Error EmailAlreadyExists => Error.Conflict("UpdateProfile.EmailAlreadyExists", "Email is already in use by another user");
    }
}