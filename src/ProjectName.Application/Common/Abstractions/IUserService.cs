using ProjectName.Application.Identity.Services.Users.Dtos;
using ProjectName.Domain.Common.ResultPattern;

namespace ProjectName.Application.Common.Abstractions;

public interface IUserService
{
    /// <summary>
    /// Retrieves the currently authenticated user.
    /// </summary>
    /// <remarks>This method is typically used to obtain information about the user currently authenticated in
    /// the system. Ensure that the caller has a valid authentication context before invoking this method.</remarks>
    /// <returns>A <see cref="Task{TResult}"/> representing the asynchronous operation.  The result contains a <see
    /// cref="Result"/> object wrapping an <see cref="AuthenticatedUserDto"/>  that represents the authenticated
    /// user, or an error if the operation fails.</returns>
    Task<Result<AuthenticatedUserDto>> GetAuthenticatedUserAsync();

    /// <summary>
    /// Changes the user's password to a new value after verifying the current password.
    /// </summary>
    /// <remarks>This method validates the current password before updating it to the new password.  Ensure
    /// that the <paramref name="newPassword"/> meets the application's password complexity requirements.</remarks>
    /// <param name="currentPassword">The user's current password. This must match the existing password to proceed.</param>
    /// <param name="newPassword">The new password to set. Must meet the required password policy.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>A <see cref="Result"/> object indicating the success or failure of the operation.  The result contains error
    /// details if the operation fails.</returns>
    Task<Result> ChangePasswordAsync(string currentPassword, string newPassword, CancellationToken cancellationToken);

    /// <summary>
    /// Asynchronously creates a new user based on the provided user data.
    /// </summary>
    /// <remarks>The method validates the provided user data before attempting to create the user. If the
    /// operation is canceled via the <paramref name="cancellationToken"/>, the task will complete in a canceled
    /// state.</remarks>
    /// <param name="userDto">The data transfer object containing the user's details. Must not be null.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests. If the operation is canceled, the task will be terminated.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains a <see cref="Result{T}"/> object
    /// with the ID of the newly created user if the operation succeeds, or an error message if it fails.</returns>
    Task<Result<string>> CreateUserAsync(UserDto userDto, CancellationToken cancellationToken);

    /// <summary>
    /// Updates the details of an existing user asynchronously.
    /// </summary>
    /// <remarks>This method performs validation on the provided user data and updates the user record in the
    /// system. Ensure that the <paramref name="cancellationToken"/> is properly handled in scenarios where the
    /// operation may need to be canceled.</remarks>
    /// <param name="userDto">An object containing the updated user information. Must not be null.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests. Passing a canceled token will result in the operation being
    /// canceled.</param>
    /// <returns>A <see cref="Result"/> object indicating the success or failure of the update operation.  The result contains
    /// error details if the operation fails.</returns>
    Task<Result> UpdateUserAsync(UserUpdateDto userDto, CancellationToken cancellationToken);

    /// <summary>
    /// Blocks a user by their unique identifier.
    /// </summary>
    /// <remarks>This method is asynchronous and should be awaited. Blocking a user may involve updating 
    /// persistent storage or notifying other systems, depending on the implementation.</remarks>
    /// <param name="id">The unique identifier of the user to block. This value cannot be null or empty.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests. If the operation is canceled, the task will be terminated.</param>
    /// <returns>A <see cref="Result"/> object indicating the success or failure of the operation.  The result contains details
    /// about the operation's outcome.</returns>
    Task<Result> BlockUserAsync(string id, CancellationToken cancellationToken);

    /// <summary>
    /// Activates a user account with the specified identifier.
    /// </summary>
    /// <remarks>This method attempts to activate the user account associated with the provided identifier. If
    /// the operation is successful, the returned <see cref="Result"/> will indicate success. Otherwise, it will contain
    /// details about the failure.</remarks>
    /// <param name="id">The unique identifier of the user to activate. Cannot be null or empty.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>A <see cref="Result"/> object indicating the success or failure of the operation.</returns>
    Task<Result> ActivateUserAsync(string id, CancellationToken cancellationToken);

    /// <summary>
    /// Updates the profile of the currently authenticated user.
    /// </summary>
    /// <remarks>This method updates the profile information (name and email) for the user currently authenticated in
    /// the system. The user's identity is retrieved from the current authentication context.</remarks>
    /// <param name="userProfileDto">An object containing the updated profile information. Must not be null.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>A <see cref="Result"/> object indicating the success or failure of the operation.</returns>
    Task<Result> UpdateProfileAsync(UserProfileUpdateDto userProfileDto, CancellationToken cancellationToken);
}