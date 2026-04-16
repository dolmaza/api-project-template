using ProjectName.Domain.Common.ResultPattern;

namespace ProjectName.Application.Identity.Services.Account;

public static class AccountErrors
{
    public static Error SendVerificationCodeFailed => Error.Failure("SendVerificationCodeFailed", "Couldn't send verification code. Email is not provided");

    public static class RegisterUser
    {
        public static Error UserExists => Error.Conflict("RegisterUser.UserExists", $"User exists with provided Email or UserName");

        public static Error FailedSaveUser => Error.Failure("RegisterUser.FailedSaveUser", "Error while saving user in database");

        public static Error FailedAddRoleToUser => Error.Failure("RegisterUser.FailedAddRoleToUser", "Error while saving user role in database");

        public static Error FailedAddClaimsToUser => Error.Failure("RegisterUser.FailedAddClaimsToUser", "Error while saving user claims in database");

        public static Error FailedAddPasswordToUser => Error.Failure("RegisterUser.FailedAddPasswordToUser", "Error while saving user password in database");

    }

    public static class VerifyRegistrationToken
    {
        public static Error UserNotFound => Error.NotFound("VerifyRegistrationToken.UserNotFound", "Couldn't find user");

        public static Error UserAlreadyVerified => Error.Conflict("VerifyRegistrationToken.UserAlreadyVerified", "User already verified account");

        public static Error WrongOtp => Error.Failure("VerifyRegistrationToken.WrongOtp", "Provided otp is not valid");

        public static Error FailedUserUpdate => Error.Failure("VerifyRegistrationToken.FailedUserUpdate", "Error while updating user in database.");

        public static Error FailedAddClaimsToUser => Error.Failure("VerifyRegistrationToken.FailedAddClaimsToUser", "Error while adding claims to user.");

    }

    public static class ResendVerifyRegistration
    {
        public static Error UserNotFound => Error.NotFound("ResendVerifyRegistration.UserNotFound", "Couldn't find user");

        public static Error UserAlreadyVerified => Error.Conflict("ResendVerifyRegistration.UserAlreadyVerified", "User already verified account");

    }

    public static class SendPasswordRecoveryCode
    {
        public static Error UserNotFound => Error.NotFound("ResendVerifyRegistration.UserNotFound", "Couldn't find user");
    }

    public static class VerifyPasswordRecoveryOtp
    {
        public static Error UserNotFound => Error.NotFound("VerifyPasswordRecoveryOtp.UserNotFound", "Couldn't find user");

        public static Error WrongOtp => Error.Failure("VerifyRegistrationToken.WrongOtp", "Provided otp is not valid");

    }

    public class PasswordRecovery
    {
        public static Error UserNotFound => Error.NotFound("PasswordRecovery.UserNotFound", "Couldn't find user");

        public static Error FailedResetPassword => Error.Failure("PasswordRecovery.FailedResetPassword", "Password reset failed. This could be due to an invalid or expired token, or the password not meeting security requirements.");
        
        public static Error InvalidToken => Error.Failure("PasswordRecovery.InvalidToken", "The password reset token is invalid or has expired. Please request a new password reset.");
        
        public static Error PasswordPolicyViolation => Error.Failure("PasswordRecovery.PasswordPolicyViolation", "The new password does not meet the required security criteria. Password must be at least 8 characters long and contain uppercase, lowercase, and numeric characters.");
    }

    public static class GetAuthenticatedUser
    {
        public static Error UserNotFound => Error.NotFound("GetAuthenticatedUser.UserNotFound", "Couldn't find user");
    }
}