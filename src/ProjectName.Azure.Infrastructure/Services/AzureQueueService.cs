using System.Collections.Concurrent;
using System.Text.Json;
using Azure.Storage.Queues;
using Azure.Storage.Queues.Models;
using Microsoft.Extensions.Logging;
using ProjectName.Application.Common.Abstractions;
using ProjectName.Application.Common.Abstractions.Storage;
using ProjectName.Domain.Common.ResultPattern;

namespace ProjectName.Azure.Infrastructure.Services;

/// <summary>
/// Azure Queue Storage implementation of <see cref="IQueueService"/>.
/// </summary>
public class AzureQueueService(
    QueueServiceClient queueServiceClient,
    ILogger<AzureQueueService> logger) : IQueueService
{
    private static readonly ConcurrentDictionary<string, bool> EnsuredQueues = new();
    private static readonly JsonSerializerOptions JsonOptions = new()
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            PropertyNameCaseInsensitive = true
        };
    
    /// <inheritdoc />
    public async Task<Result<string>> SendMessageAsync(
        string queueName, QueueMessageRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var queueClient = await EnsureQueueExistsAsync(queueName, cancellationToken);

            var response = await queueClient.SendMessageAsync(
                request.Content,
                request.VisibilityTimeout,
                request.TimeToLive,
                cancellationToken);

            logger.LogInformation("Sent message '{MessageId}' to queue '{QueueName}'",
                response.Value.MessageId, queueName);

            return Result<string>.Success(response.Value.MessageId);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to send message to queue '{QueueName}'", queueName);

            return Result<string>.Failure(
                Error.Failure("Queue.SendFailed", $"Failed to send message to queue '{queueName}': {ex.Message}"));
        }
    }

    /// <inheritdoc />
    public async Task<Result<QueueMessageResult?>> ReceiveMessageAsync(
        string queueName, CancellationToken cancellationToken)
    {
        try
        {
            var queueClient = GetQueueClient(queueName);

            var response = await queueClient.ReceiveMessageAsync(cancellationToken: cancellationToken);

            if (response.Value is null)
            {
                return Result<QueueMessageResult?>.Success(null);
            }

            var result = MapToQueueMessageResult(response.Value);
            return Result<QueueMessageResult?>.Success(result);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to receive message from queue '{QueueName}'", queueName);

            return Result<QueueMessageResult?>.Failure(
                Error.Failure("Queue.ReceiveFailed", $"Failed to receive message from queue '{queueName}': {ex.Message}"));
        }
    }

    /// <inheritdoc />
    public async Task<Result<IReadOnlyList<QueueMessageResult>>> ReceiveMessagesAsync(
        string queueName, int maxMessages, CancellationToken cancellationToken)
    {
        try
        {
            var queueClient = GetQueueClient(queueName);

            var response = await queueClient.ReceiveMessagesAsync(
                maxMessages, cancellationToken: cancellationToken);

            var results = response.Value
                .Select(MapToQueueMessageResult)
                .ToList()
                .AsReadOnly();

            return Result<IReadOnlyList<QueueMessageResult>>.Success(results);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to receive messages from queue '{QueueName}'", queueName);

            return Result<IReadOnlyList<QueueMessageResult>>.Failure(
                Error.Failure("Queue.ReceiveFailed", $"Failed to receive messages from queue '{queueName}': {ex.Message}"));
        }
    }

    /// <inheritdoc />
    public async Task<Result<string>> SendMessageAsync<T>(
        string queueName, T message, CancellationToken cancellationToken) where T : class
    {
        try
        {
            var json = JsonSerializer.Serialize(message, JsonOptions);
            var queueClient = await EnsureQueueExistsAsync(queueName, cancellationToken);

            var response = await queueClient.SendMessageAsync(json, cancellationToken: cancellationToken);

            logger.LogInformation("Sent typed message '{MessageId}' to queue '{QueueName}'",
                response.Value.MessageId, queueName);

            return Result<string>.Success(response.Value.MessageId);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to send typed message to queue '{QueueName}'", queueName);

            return Result<string>.Failure(
                Error.Failure("Queue.SendFailed", $"Failed to send message to queue '{queueName}': {ex.Message}"));
        }
    }

    /// <inheritdoc />
    public async Task<Result<QueueMessageResult<T>?>> ReceiveMessageAsync<T>(
        string queueName, CancellationToken cancellationToken) where T : class
    {
        try
        {
            var queueClient = GetQueueClient(queueName);

            var response = await queueClient.ReceiveMessageAsync(cancellationToken: cancellationToken);

            if (response.Value is null)
            {
                return Result<QueueMessageResult<T>?>.Success(null);
            }

            var typed = MapToTypedQueueMessageResult<T>(response.Value);
            return Result<QueueMessageResult<T>?>.Success(typed);
        }
        catch (JsonException ex)
        {
            logger.LogError(ex, "Failed to deserialize message from queue '{QueueName}' to type '{Type}'",
                queueName, typeof(T).Name);

            return Result<QueueMessageResult<T>?>.Failure(
                Error.Failure("Queue.DeserializationFailed", $"Failed to deserialize message from queue '{queueName}' to {typeof(T).Name}: {ex.Message}"));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to receive typed message from queue '{QueueName}'", queueName);

            return Result<QueueMessageResult<T>?>.Failure(
                Error.Failure("Queue.ReceiveFailed", $"Failed to receive message from queue '{queueName}': {ex.Message}"));
        }
    }

    /// <inheritdoc />
    public async Task<Result<IReadOnlyList<QueueMessageResult<T>>>> ReceiveMessagesAsync<T>(
        string queueName, int maxMessages, CancellationToken cancellationToken) where T : class
    {
        try
        {
            var queueClient = GetQueueClient(queueName);

            var response = await queueClient.ReceiveMessagesAsync(
                maxMessages, cancellationToken: cancellationToken);

            var results = response.Value
                .Select(MapToTypedQueueMessageResult<T>)
                .ToList()
                .AsReadOnly();

            return Result<IReadOnlyList<QueueMessageResult<T>>>.Success(results);
        }
        catch (JsonException ex)
        {
            logger.LogError(ex, "Failed to deserialize messages from queue '{QueueName}' to type '{Type}'",
                queueName, typeof(T).Name);

            return Result<IReadOnlyList<QueueMessageResult<T>>>.Failure(
                Error.Failure("Queue.DeserializationFailed", $"Failed to deserialize messages from queue '{queueName}' to {typeof(T).Name}: {ex.Message}"));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to receive typed messages from queue '{QueueName}'", queueName);

            return Result<IReadOnlyList<QueueMessageResult<T>>>.Failure(
                Error.Failure("Queue.ReceiveFailed", $"Failed to receive messages from queue '{queueName}': {ex.Message}"));
        }
    }

    /// <inheritdoc />
    public async Task<Result> DeleteMessageAsync(
        string queueName, string messageId, string popReceipt, CancellationToken cancellationToken)
    {
        try
        {
            var queueClient = GetQueueClient(queueName);

            await queueClient.DeleteMessageAsync(messageId, popReceipt, cancellationToken);

            logger.LogInformation("Deleted message '{MessageId}' from queue '{QueueName}'",
                messageId, queueName);

            return Result.Success();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to delete message '{MessageId}' from queue '{QueueName}'",
                messageId, queueName);

            return Result.Failure(
                Error.Failure("Queue.DeleteFailed", $"Failed to delete message '{messageId}' from queue '{queueName}': {ex.Message}"));
        }
    }
    
    private QueueClient GetQueueClient(string queueName) =>
        queueServiceClient.GetQueueClient(queueName);

    private async Task<QueueClient> EnsureQueueExistsAsync(string queueName, CancellationToken cancellationToken)
    {
        var queueClient = GetQueueClient(queueName);

        if (EnsuredQueues.TryAdd(queueName, true))
        {
            try
            {
                await queueClient.CreateIfNotExistsAsync(cancellationToken: cancellationToken);
            }
            catch
            {
                EnsuredQueues.TryRemove(queueName, out _);
                throw;
            }
        }

        return queueClient;
    }

    private static QueueMessageResult MapToQueueMessageResult(QueueMessage message) =>
        new(
            MessageId: message.MessageId,
            Content: message.Body.ToString(),
            ReceiptHandle: message.PopReceipt,
            ReceiveCount: message.DequeueCount,
            InsertedOn: message.InsertedOn);

    private static QueueMessageResult<T> MapToTypedQueueMessageResult<T>(QueueMessage message) where T : class
    {
        var content = JsonSerializer.Deserialize<T>(message.Body.ToString(), JsonOptions)
                      ?? throw new JsonException($"Deserialization of queue message to {typeof(T).Name} returned null.");

        return new QueueMessageResult<T>(
            MessageId: message.MessageId,
            Content: content,
            ReceiptHandle: message.PopReceipt,
            ReceiveCount: message.DequeueCount,
            InsertedOn: message.InsertedOn);
    }
}
