namespace ProjectName.Application.Common.Abstractions.Storage;

/// <summary>
/// Represents a request to send a message to a queue.
/// </summary>
/// <param name="Content">The message body content.</param>
/// <param name="VisibilityTimeout">
/// Optional duration the message should be invisible to other consumers after being enqueued.
/// </param>
/// <param name="TimeToLive">
/// Optional maximum time the message is allowed to stay in the queue before expiring.
/// </param>
public record QueueMessageRequest(
    string Content,
    TimeSpan? VisibilityTimeout = null,
    TimeSpan? TimeToLive = null);
