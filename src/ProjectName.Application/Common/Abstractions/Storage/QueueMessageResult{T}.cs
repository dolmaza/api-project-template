namespace ProjectName.Application.Common.Abstractions.Storage;

/// <summary>
/// Represents a typed message received from a queue, with the body deserialized to <typeparamref name="T"/>.
/// </summary>
/// <typeparam name="T">The type the message body was deserialized to.</typeparam>
/// <param name="MessageId">The unique identifier of the message.</param>
/// <param name="Content">The deserialized message body.</param>
/// <param name="ReceiptHandle">
/// A receipt token required to delete or update the message after receiving it.
/// </param>
/// <param name="ReceiveCount">The number of times this message has been received from the queue.</param>
/// <param name="InsertedOn">The date and time the message was inserted into the queue, if available.</param>
public record QueueMessageResult<T>(
    string MessageId,
    T Content,
    string ReceiptHandle,
    long ReceiveCount,
    DateTimeOffset? InsertedOn);
