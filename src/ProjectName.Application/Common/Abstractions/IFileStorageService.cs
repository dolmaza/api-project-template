using ProjectName.Application.Common.Abstractions.Storage;
using ProjectName.Domain.Common.ResultPattern;

namespace ProjectName.Application.Common.Abstractions;

/// <summary>
/// Provider-agnostic abstraction for file (blob/object) storage operations.
/// Implementations may target Azure Blob Storage, AWS S3, local disk, or any other provider.
/// </summary>
public interface IFileStorageService
{
    /// <summary>
    /// Uploads a file to the specified (or default) container.
    /// </summary>
    /// <param name="request">The upload request containing the file content, name, and metadata.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The URI or path of the uploaded file on success; an error on failure.</returns>
    Task<Result<string>> UploadAsync(FileUploadRequest request, CancellationToken cancellationToken);

    /// <summary>
    /// Downloads a file from the specified container.
    /// </summary>
    /// <param name="containerName">The container or bucket name.</param>
    /// <param name="fileName">The name of the file to download.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A <see cref="FileDownloadResult"/> containing the file stream and metadata on success.</returns>
    Task<Result<FileDownloadResult>> DownloadAsync(string containerName, string fileName, CancellationToken cancellationToken);

    /// <summary>
    /// Deletes a file from the specified container.
    /// </summary>
    /// <param name="containerName">The container or bucket name.</param>
    /// <param name="fileName">The name of the file to delete.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Success if the file was deleted; an error on failure.</returns>
    Task<Result> DeleteAsync(string containerName, string fileName, CancellationToken cancellationToken);

    /// <summary>
    /// Checks whether a file exists in the specified container.
    /// </summary>
    /// <param name="containerName">The container or bucket name.</param>
    /// <param name="fileName">The name of the file to check.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns><c>true</c> if the file exists; <c>false</c> otherwise.</returns>
    Task<Result<bool>> ExistsAsync(string containerName, string fileName, CancellationToken cancellationToken);

    /// <summary>
    /// Retrieves metadata for a file in the specified container.
    /// </summary>
    /// <param name="containerName">The container or bucket name.</param>
    /// <param name="fileName">The name of the file.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The file metadata on success; an error on failure (e.g., file not found).</returns>
    Task<Result<FileMetadata>> GetMetadataAsync(string containerName, string fileName, CancellationToken cancellationToken);
}
