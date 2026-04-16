using Azure.Storage.Blobs;
using Azure.Storage.Queues;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using ProjectName.Application.Common.Constants;

namespace ProjectName.Azure.Infrastructure;

/// <summary>
/// Ensures that all required Azure Storage blob containers and queues
/// exist when the application starts. Creates them if they do not exist.
/// </summary>
public static class AzureStorageInitializer
{
    /// <summary>
    /// Blob container names that must exist before the application handles requests.
    /// </summary>
    private static readonly string[] RequiredContainers =
    [
    ];

    /// <summary>
    /// Queue names that must exist before the application handles requests.
    /// </summary>
    private static readonly string[] RequiredQueues =
    [
    ];

    /// <summary>
    /// Creates all required blob containers and queues if they do not already exist.
    /// </summary>
    public static async Task InitializeAsync(IApplicationBuilder app)
    {
        using var scope = app.ApplicationServices.CreateScope();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<BlobServiceClient>>();

        await EnsureBlobContainersAsync(scope.ServiceProvider, logger);
        await EnsureQueuesAsync(scope.ServiceProvider, logger);
    }

    private static async Task EnsureBlobContainersAsync(IServiceProvider services, ILogger logger)
    {
        var blobServiceClient = services.GetRequiredService<BlobServiceClient>();

        foreach (var containerName in RequiredContainers)
        {
            try
            {
                var containerClient = blobServiceClient.GetBlobContainerClient(containerName);
                var response = await containerClient.CreateIfNotExistsAsync();

                if (response is not null)
                {
                    logger.LogInformation("Created blob container '{ContainerName}'", containerName);
                }
                else
                {
                    logger.LogDebug("Blob container '{ContainerName}' already exists", containerName);
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to ensure blob container '{ContainerName}' exists", containerName);
                throw;
            }
        }
    }

    private static async Task EnsureQueuesAsync(IServiceProvider services, ILogger logger)
    {
        var queueServiceClient = services.GetRequiredService<QueueServiceClient>();

        foreach (var queueName in RequiredQueues)
        {
            try
            {
                var queueClient = queueServiceClient.GetQueueClient(queueName);
                var response = await queueClient.CreateIfNotExistsAsync();

                if (response is not null)
                {
                    logger.LogInformation("Created queue '{QueueName}'", queueName);
                }
                else
                {
                    logger.LogDebug("Queue '{QueueName}' already exists", queueName);
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to ensure queue '{QueueName}' exists", queueName);
                throw;
            }
        }
    }
}

