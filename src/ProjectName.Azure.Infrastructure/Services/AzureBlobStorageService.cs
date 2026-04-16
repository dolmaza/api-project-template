using Azure;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Microsoft.Extensions.Logging;
using ProjectName.Application.Common.Abstractions;
using ProjectName.Application.Common.Abstractions.Storage;
using ProjectName.Domain.Common.ResultPattern;

namespace ProjectName.Azure.Infrastructure.Services;

/// <summary>
/// Azure Blob Storage implementation of <see cref="IFileStorageService"/>.
/// </summary>
public class AzureBlobStorageService(
    BlobServiceClient blobServiceClient,
    ILogger<AzureBlobStorageService> logger) : IFileStorageService
{
    private const string DefaultContainer = "default";

    /// <inheritdoc />
    public async Task<Result<string>> UploadAsync(FileUploadRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var containerClient = blobServiceClient.GetBlobContainerClient(request.ContainerName ?? DefaultContainer);
            await containerClient.CreateIfNotExistsAsync(cancellationToken: cancellationToken);

            var blobClient = containerClient.GetBlobClient(request.FileName);

            var options = new BlobUploadOptions
            {
                HttpHeaders = new BlobHttpHeaders { ContentType = request.ContentType },
                Metadata = request.Metadata
            };

            await blobClient.UploadAsync(request.Content, options, cancellationToken);

            logger.LogInformation("Uploaded blob '{FileName}' to container '{Container}'",
                request.FileName, request.ContainerName ?? DefaultContainer);

            return Result<string>.Success(blobClient.Uri.ToString());
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to upload blob '{FileName}' to container '{Container}'",
                request.FileName, request.ContainerName ?? DefaultContainer);

            return Result<string>.Failure(
                Error.Failure("FileStorage.UploadFailed", $"Failed to upload file '{request.FileName}': {ex.Message}"));
        }
    }

    /// <inheritdoc />
    public async Task<Result<FileDownloadResult>> DownloadAsync(
        string containerName, string fileName, CancellationToken cancellationToken)
    {
        try
        {
            var blobClient = blobServiceClient
                .GetBlobContainerClient(containerName)
                .GetBlobClient(fileName);

            var response = await blobClient.DownloadStreamingAsync(cancellationToken: cancellationToken);

            var result = new FileDownloadResult(
                Content: response.Value.Content,
                ContentType: response.Value.Details.ContentType,
                FileName: fileName);

            return Result<FileDownloadResult>.Success(result);
        }
        catch (RequestFailedException ex) when (ex.Status == 404)
        {
            return Result<FileDownloadResult>.Failure(
                Error.NotFound("FileStorage.NotFound", $"File '{fileName}' not found in container '{containerName}'."));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to download blob '{FileName}' from container '{Container}'",
                fileName, containerName);

            return Result<FileDownloadResult>.Failure(
                Error.Failure("FileStorage.DownloadFailed", $"Failed to download file '{fileName}': {ex.Message}"));
        }
    }

    /// <inheritdoc />
    public async Task<Result> DeleteAsync(
        string containerName, string fileName, CancellationToken cancellationToken)
    {
        try
        {
            var blobClient = blobServiceClient
                .GetBlobContainerClient(containerName)
                .GetBlobClient(fileName);

            await blobClient.DeleteIfExistsAsync(cancellationToken: cancellationToken);

            logger.LogInformation("Deleted blob '{FileName}' from container '{Container}'",
                fileName, containerName);

            return Result.Success();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to delete blob '{FileName}' from container '{Container}'",
                fileName, containerName);

            return Result.Failure(
                Error.Failure("FileStorage.DeleteFailed", $"Failed to delete file '{fileName}': {ex.Message}"));
        }
    }

    /// <inheritdoc />
    public async Task<Result<bool>> ExistsAsync(
        string containerName, string fileName, CancellationToken cancellationToken)
    {
        try
        {
            var blobClient = blobServiceClient
                .GetBlobContainerClient(containerName)
                .GetBlobClient(fileName);

            var response = await blobClient.ExistsAsync(cancellationToken);

            return Result<bool>.Success(response.Value);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to check existence of blob '{FileName}' in container '{Container}'",
                fileName, containerName);

            return Result<bool>.Failure(
                Error.Failure("FileStorage.ExistsFailed", $"Failed to check existence of file '{fileName}': {ex.Message}"));
        }
    }

    /// <inheritdoc />
    public async Task<Result<FileMetadata>> GetMetadataAsync(
        string containerName, string fileName, CancellationToken cancellationToken)
    {
        try
        {
            var blobClient = blobServiceClient
                .GetBlobContainerClient(containerName)
                .GetBlobClient(fileName);

            var properties = await blobClient.GetPropertiesAsync(cancellationToken: cancellationToken);

            var metadata = new FileMetadata(
                FileName: fileName,
                ContentLength: properties.Value.ContentLength,
                ContentType: properties.Value.ContentType,
                LastModified: properties.Value.LastModified,
                Metadata: properties.Value.Metadata);

            return Result<FileMetadata>.Success(metadata);
        }
        catch (RequestFailedException ex) when (ex.Status == 404)
        {
            return Result<FileMetadata>.Failure(
                Error.NotFound("FileStorage.NotFound", $"File '{fileName}' not found in container '{containerName}'."));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to get metadata for blob '{FileName}' in container '{Container}'",
                fileName, containerName);

            return Result<FileMetadata>.Failure(
                Error.Failure("FileStorage.MetadataFailed", $"Failed to get metadata for file '{fileName}': {ex.Message}"));
        }
    }
}
