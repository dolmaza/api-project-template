namespace ProjectName.Application.Common.Abstractions.Storage;

/// <summary>
/// Represents metadata about a file stored in cloud storage.
/// </summary>
/// <param name="FileName">The name of the file.</param>
/// <param name="ContentLength">The size of the file in bytes.</param>
/// <param name="ContentType">The MIME content type of the file.</param>
/// <param name="LastModified">The date and time the file was last modified, if available.</param>
/// <param name="Metadata">Key-value metadata associated with the file.</param>
public record FileMetadata(
    string FileName,
    long ContentLength,
    string ContentType,
    DateTimeOffset? LastModified,
    IDictionary<string, string> Metadata);
