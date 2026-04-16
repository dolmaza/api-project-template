namespace ProjectName.Application.Common.Abstractions.Storage;

/// <summary>
/// Represents a message received from a queue.
/// </summary>
/// <param name="MessageId">The unique identifier of the message.</param>
/// <param name="Content">The message body content.</param>
/// <param name="ReceiptHandle">
/// A provider-agnostic receipt token required to delete or update the message after receiving it.
/// </param>
/// <param name="ReceiveCount">The number of times this message has been received/dequeued.</param>
/// <param name="InsertedOn">The date and time the message was inserted into the queue, if available.</param>
public record QueueMessageResult(
    string MessageId,
    string Content,
    string ReceiptHandle,
    long ReceiveCount,
    DateTimeOffset? InsertedOn);
