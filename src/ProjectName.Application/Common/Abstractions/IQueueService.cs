using ProjectName.Application.Common.Abstractions.Storage;
using ProjectName.Domain.Common.ResultPattern;

namespace ProjectName.Application.Common.Abstractions;

/// <summary>
/// Provider-agnostic abstraction for queue messaging operations.
/// Implementations may target Azure Queue Storage, AWS SQS, RabbitMQ, or any other provider.
/// </summary>
public interface IQueueService
{
    /// <summary>
    /// Sends a message to the specified queue.
    /// </summary>
    /// <param name="queueName">The name of the target queue.</param>
    /// <param name="request">The message request containing the content and optional delivery options.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The message identifier on success; an error on failure.</returns>
    Task<Result<string>> SendMessageAsync(string queueName, QueueMessageRequest request, CancellationToken cancellationToken);

    /// <summary>
    /// Receives a single message from the specified queue.
    /// The message becomes invisible to other consumers for the provider's default visibility timeout.
    /// </summary>
    /// <param name="queueName">The name of the queue to receive from.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The received message, or <c>null</c> if the queue is empty.</returns>
    Task<Result<QueueMessageResult?>> ReceiveMessageAsync(string queueName, CancellationToken cancellationToken);

    /// <summary>
    /// Receives a batch of messages from the specified queue.
    /// </summary>
    /// <param name="queueName">The name of the queue to receive from.</param>
    /// <param name="maxMessages">The maximum number of messages to retrieve (provider may impose an upper bound).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A read-only list of received messages (may be empty if the queue is empty).</returns>
    Task<Result<IReadOnlyList<QueueMessageResult>>> ReceiveMessagesAsync(string queueName, int maxMessages, CancellationToken cancellationToken);

    /// <summary>
    /// Serializes <paramref name="message"/> to JSON and sends it to the specified queue.
    /// </summary>
    /// <typeparam name="T">The message payload type.</typeparam>
    /// <param name="queueName">The name of the target queue.</param>
    /// <param name="message">The object to serialize and enqueue.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The message identifier on success; an error on failure.</returns>
    Task<Result<string>> SendMessageAsync<T>(string queueName, T message, CancellationToken cancellationToken) where T : class;

    /// <summary>
    /// Receives a single message from the specified queue and deserializes the body to <typeparamref name="T"/>.
    /// </summary>
    /// <typeparam name="T">The expected message payload type.</typeparam>
    /// <param name="queueName">The name of the queue to receive from.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The deserialized message, or <c>null</c> if the queue is empty.</returns>
    Task<Result<QueueMessageResult<T>?>> ReceiveMessageAsync<T>(string queueName, CancellationToken cancellationToken) where T : class;

    /// <summary>
    /// Receives a batch of messages from the specified queue and deserializes each body to <typeparamref name="T"/>.
    /// </summary>
    /// <typeparam name="T">The expected message payload type.</typeparam>
    /// <param name="queueName">The name of the queue to receive from.</param>
    /// <param name="maxMessages">The maximum number of messages to retrieve.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A read-only list of deserialized messages (may be empty if the queue is empty).</returns>
    Task<Result<IReadOnlyList<QueueMessageResult<T>>>> ReceiveMessagesAsync<T>(string queueName, int maxMessages, CancellationToken cancellationToken) where T : class;

    /// <summary>
    /// Deletes a previously received message from the queue, acknowledging successful processing.
    /// </summary>
    /// <param name="queueName">The name of the queue.</param>
    /// <param name="messageId">The message identifier.</param>
    /// <param name="receiptHandle">The provider-specific receipt handle or acknowledgment token obtained when the message was received.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Success if the message was deleted; an error on failure.</returns>
    Task<Result> DeleteMessageAsync(string queueName, string messageId, string receiptHandle, CancellationToken cancellationToken);
}
