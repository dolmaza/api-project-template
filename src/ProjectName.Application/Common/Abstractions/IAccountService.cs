using ProjectName.Application.Identity.Services.Account.Dtos;
using ProjectName.Domain.Common.ResultPattern;

namespace ProjectName.Application.Common.Abstractions;

public interface IAccountService
{
    /// <summary>
    /// Registers a new user asynchronously based on the provided registration details.
    /// </summary>
    /// <remarks>The method validates the provided registration details and attempts to register the user.  If
    /// the registration is successful, the result will include the details of the newly registered user. Otherwise, the
    /// result will indicate the failure reason.</remarks>
    /// <param name="registerUserDto">The user registration details, including required information such as username, email, and password.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests, allowing the operation to be canceled.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains a <see cref="Result"/> object
    /// with a <see cref="RegisterUserResultDto"/> indicating the outcome of the registration process.</returns>
    Task<Result<RegisterUserResultDto>> RegisterUserAsync(RegisterUserDto registerUserDto, CancellationToken cancellationToken);

    /// <summary>
    /// Verifies the provided one-time password (OTP) for the specified user.
    /// </summary>
    /// <param name="userName">The username of the user whose OTP is being verified. Cannot be null or empty.</param>
    /// <param name="otp">The one-time password to verify. Cannot be null or empty.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>A <see cref="Result"/> object indicating the success or failure of the verification process.</returns>
    Task<Result> VerifyRegistrationTokenAsync(string userName, string otp, CancellationToken cancellationToken);

    /// <summary>
    /// Resends the verification token for a user's registration.
    /// </summary>
    /// <remarks>This method is typically used to resend a registration verification token to a user who has
    /// not yet completed the registration process. Ensure that the provided <paramref name="userName"/> corresponds to
    /// a valid user in the system.</remarks>
    /// <param name="userName">The username of the user for whom the verification token should be resent. Cannot be null or empty.</param>
    /// <param name="purpose">Purpose of the issuing a token (VerifyOwner or PasswordRecovery)</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>A <see cref="Result"/> indicating the outcome of the operation.  The result may include information about
    /// success or failure, such as whether the user exists or if the token was successfully resent.</returns>
    Task<Result> ResendVerificationTokenAsync(string userName, string purpose, CancellationToken cancellationToken);

    /// <summary>
    /// Sends a password recovery code to the specified email address.
    /// </summary>
    /// <remarks>This method initiates the process of password recovery by sending a recovery code to the
    /// provided email address. The caller is responsible for ensuring that the email address is valid and associated
    /// with an account.</remarks>
    /// <param name="email">The email address to which the password recovery code will be sent. Must be a valid, non-null, and non-empty
    /// email address.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>A <see cref="Result"/> indicating the success or failure of the operation. The result may contain additional
    /// details about the outcome.</returns>
    Task<Result> SendPasswordRecoveryCodeAsync(string email, CancellationToken cancellationToken);

    /// <summary>
    /// Verifies the provided OTP (One-Time Password) for password recovery associated with the specified email address.
    /// </summary>
    /// <remarks>This method validates the OTP against the email address for a password recovery process. 
    /// Ensure that the OTP is provided within its validity period to avoid verification failure.</remarks>
    /// <param name="email">The email address associated with the password recovery request. Cannot be null or empty.</param>
    /// <param name="otp">The one-time password to verify. Cannot be null or empty.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>A <see cref="Result{T}"/> containing a success message if the OTP is valid, or an error message if the
    /// verification fails.</returns>
    Task<Result<string>> VerifyPasswordRecoveryOtpAsync(string email, string otp, CancellationToken cancellationToken);

    /// <summary>
    /// Attempts to recover a user's account by resetting their password.
    /// </summary>
    /// <remarks>Ensure that the provided <paramref name="token"/> and <paramref name="email"/> are valid and
    /// match the account being recovered. The <paramref name="password"/> must comply with the system's security
    /// policies.</remarks>
    /// <param name="token">The recovery token used to validate the password reset request. Must be a valid, non-empty string.</param>
    /// <param name="email">The email address associated with the account. Must be a valid, non-empty email address.</param>
    /// <param name="password">The new password to set for the account. Must meet the system's password complexity requirements.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests. If cancellation is requested, the operation will terminate early.</param>
    /// <returns>A <see cref="Result"/> object indicating the success or failure of the password recovery operation. The result
    /// contains details about the operation, such as error messages if the recovery fails.</returns>
    Task<Result> PasswordRecoveryAsync(string token, string email, string password, CancellationToken cancellationToken);
}