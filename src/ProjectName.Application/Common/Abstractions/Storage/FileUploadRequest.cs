namespace ProjectName.Application.Common.Abstractions.Storage;

/// <summary>
/// Represents a request to upload a file to cloud storage.
/// </summary>
/// <param name="Content">The file content stream.</param>
/// <param name="FileName">The name of the file (including extension).</param>
/// <param name="ContentType">The MIME content type of the file (e.g., "image/png").</param>
/// <param name="ContainerName">
/// Optional logical container or bucket name. When <c>null</c>, the implementation uses a default container.
/// </param>
/// <param name="Metadata">Optional key-value metadata to attach to the stored file.</param>
public record FileUploadRequest(
    Stream Content,
    string FileName,
    string ContentType,
    string? ContainerName = null,
    IDictionary<string, string>? Metadata = null);
