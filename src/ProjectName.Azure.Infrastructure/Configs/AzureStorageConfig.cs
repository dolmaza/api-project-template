namespace ProjectName.Azure.Infrastructure.Configs;

/// <summary>
/// Configuration for Azure Storage connection strings.
/// Populated from Aspire-injected connection strings ("blobs" / "queues").
/// </summary>
public class AzureStorageConfig
{
    /// <summary>
    /// Connection string for Azure Blob Storage (or Azurite emulator).
    /// </summary>
    public string BlobConnectionString { get; set; } = string.Empty;

    /// <summary>
    /// Connection string for Azure Queue Storage (or Azurite emulator).
    /// </summary>
    public string QueueConnectionString { get; set; } = string.Empty;
}
