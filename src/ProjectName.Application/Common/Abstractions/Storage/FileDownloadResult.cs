namespace ProjectName.Application.Common.Abstractions.Storage;

/// <summary>
/// Represents the result of downloading a file from cloud storage.
/// The caller is responsible for disposing the <see cref="Content"/> stream.
/// </summary>
/// <param name="Content">The file content stream. Must be disposed by the consumer.</param>
/// <param name="ContentType">The MIME content type of the file.</param>
/// <param name="FileName">The name of the file.</param>
public record FileDownloadResult(
    Stream Content,
    string ContentType,
    string FileName) : IAsyncDisposable
{
    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        await Content.DisposeAsync();
        GC.SuppressFinalize(this);
    }
}
