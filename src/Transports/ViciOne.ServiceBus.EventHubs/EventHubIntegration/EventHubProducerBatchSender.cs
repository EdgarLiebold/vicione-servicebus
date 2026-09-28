using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Azure.Messaging.EventHubs;
using Azure.Messaging.EventHubs.Producer;
using ViciOne.ServiceBus.Transports;

namespace ViciOne.ServiceBus.EventHubs;

/// <summary>Prepares and sends Event Hubs messages in size- and route-compatible provider batches.</summary>
internal static class EventHubProducerBatchSender
{
    /// <summary>Validates every context before creating provider state, then sends each message through its declared route.</summary>
    /// <typeparam name="T">The message type.</typeparam>
    /// <param name="producerContext">The active producer context.</param>
    /// <param name="sendContexts">The outbound message contexts.</param>
    /// <param name="cancellationToken">Cancels validation, batch creation, or provider submission.</param>
    /// <returns>A task that completes after every provider batch has been sent.</returns>
    public static Task SendAsync<T>(
        ProducerContext producerContext,
        EventHubSendContext<T>[] sendContexts,
        CancellationToken cancellationToken)
        where T : class
    {
        return SendAsync(producerContext, sendContexts, cancellationToken, static batch => batch.Dispose());
    }

    internal static async Task SendAsync<T>(
        ProducerContext producerContext,
        EventHubSendContext<T>[] sendContexts,
        CancellationToken cancellationToken,
        Action<EventDataBatch> disposeBatch)
        where T : class
    {
        ArgumentNullException.ThrowIfNull(producerContext);
        ArgumentNullException.ThrowIfNull(sendContexts);
        ArgumentNullException.ThrowIfNull(disposeBatch);

        if (sendContexts.Length == 0)
            throw new ArgumentException("At least one send context is required.", nameof(sendContexts));

        EventHubMessageSendContext<T>[] contexts = ValidateContexts(sendContexts);
        using CancellationTokenSource operationTokenSource = CreateOperationTokenSource(contexts, cancellationToken);
        CancellationToken operationToken = operationTokenSource.Token;
        operationToken.ThrowIfCancellationRequested();

        PreparedEvent[] events = PrepareEvents(contexts, operationToken);
        SetActivityRoute(events);

        EventDataBatch? batch = null;
        Route currentRoute = default;
        var batchStartIndex = 0;

        try
        {
            for (var index = 0; index < events.Length; index++)
            {
                operationToken.ThrowIfCancellationRequested();
                PreparedEvent preparedEvent = events[index];

                if (batch is null || preparedEvent.Route != currentRoute)
                {
                    if (batch is not null)
                    {
                        await producerContext.ProduceAsync(batch, operationToken).ConfigureAwait(false);
                        MarkConfirmed(contexts, batchStartIndex, index);
                        EventDataBatch completed = batch;
                        batch = null;
                        DisposeBatchSafely(completed, disposeBatch);
                    }

                    currentRoute = preparedEvent.Route;
                    batch = await CreateBatchAsync(producerContext, currentRoute, operationToken).ConfigureAwait(false);
                    batchStartIndex = index;
                }

                if (batch.TryAdd(preparedEvent.EventData))
                    continue;

                if (batch.Count == 0)
                    throw CreateMessageTooLargeException();

                await producerContext.ProduceAsync(batch, operationToken).ConfigureAwait(false);
                MarkConfirmed(contexts, batchStartIndex, index);
                EventDataBatch completedBatch = batch;
                batch = null;
                DisposeBatchSafely(completedBatch, disposeBatch);

                batch = await CreateBatchAsync(producerContext, currentRoute, operationToken).ConfigureAwait(false);
                batchStartIndex = index;
                if (!batch.TryAdd(preparedEvent.EventData))
                    throw CreateMessageTooLargeException();
            }

            if (batch is not null)
            {
                await producerContext.ProduceAsync(batch, operationToken).ConfigureAwait(false);
                MarkConfirmed(contexts, batchStartIndex, contexts.Length);
                EventDataBatch completed = batch;
                batch = null;
                DisposeBatchSafely(completed, disposeBatch);
            }
        }
        finally
        {
            if (batch is not null)
                DisposeBatchSafely(batch, disposeBatch);
        }
    }

    static void MarkConfirmed<T>(EventHubMessageSendContext<T>[] contexts, int startIndex, int endIndex)
        where T : class
    {
        for (var index = startIndex; index < endIndex; index++)
            contexts[index].IsProviderConfirmed = true;
    }

    static void DisposeBatchSafely(EventDataBatch batch, Action<EventDataBatch> disposeBatch)
    {
        try
        {
            disposeBatch(batch);
        }
        catch (Exception disposeFailure)
        {
            try
            {
                LogContext.Error?.Log(disposeFailure, "An Event Hubs send batch failed to dispose after provider use");
            }
            catch (Exception)
            {
                // Cleanup diagnostics must not replace the provider outcome.
            }
        }
    }

    static ValueTask<EventDataBatch> CreateBatchAsync(
        ProducerContext producerContext,
        Route route,
        CancellationToken cancellationToken)
    {
        var options = new CreateBatchOptions
        {
            PartitionId = route.PartitionId,
            PartitionKey = route.PartitionKey
        };

        return producerContext.CreateBatchAsync(options, cancellationToken);
    }

    static CancellationTokenSource CreateOperationTokenSource<T>(
        EventHubMessageSendContext<T>[] contexts,
        CancellationToken cancellationToken)
        where T : class
    {
        var cancellationTokens = new CancellationToken[contexts.Length + 1];
        cancellationTokens[0] = cancellationToken;

        for (var index = 0; index < contexts.Length; index++)
            cancellationTokens[index + 1] = contexts[index].CancellationToken;

        return CancellationTokenSource.CreateLinkedTokenSource(cancellationTokens);
    }

    static InvalidOperationException CreateMessageTooLargeException()
    {
        return new InvalidOperationException("A message exceeds the maximum Event Hubs batch size.");
    }

    static PreparedEvent[] PrepareEvents<T>(
        EventHubMessageSendContext<T>[] contexts,
        CancellationToken cancellationToken)
        where T : class
    {
        NewId[] conversationIds = NewId.Next(contexts.Length);
        var events = new PreparedEvent[contexts.Length];

        for (var index = 0; index < contexts.Length; index++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            EventHubMessageSendContext<T> context = contexts[index];
            context.ConversationId ??= conversationIds[index].ToGuid();

            var eventData = new EventData(context.Body.ToArray());
            eventData.Properties.Set(context.Headers);

            if (context.MessageId.HasValue)
                eventData.MessageId = context.MessageId.Value.ToString("N");

            if (context.CorrelationId.HasValue)
                eventData.CorrelationId = context.CorrelationId.Value.ToString("N");

            eventData.ContentType = (context.ContentType
                ?? throw new InvalidOperationException("A content type is required before an Event Hub message can be sent.")).ToString();

            events[index] = new PreparedEvent(eventData, CreateRoute(context, index));
        }

        return events;
    }

    static Route CreateRoute<T>(EventHubMessageSendContext<T> context, int index)
        where T : class
    {
        string? partitionId = string.IsNullOrEmpty(context.PartitionId) ? null : context.PartitionId;
        string? partitionKey = string.IsNullOrEmpty(context.PartitionKey) ? null : context.PartitionKey;

        if (partitionId is not null && partitionKey is not null)
        {
            throw new ArgumentException(
                $"Send context at index {index} specifies both PartitionId and PartitionKey.",
                "sendContexts");
        }

        return new Route(partitionId, partitionKey);
    }

    static void SetActivityRoute(PreparedEvent[] events)
    {
        Activity? activity = Activity.Current;
        if (!(activity?.IsAllDataRequested ?? false))
            return;

        Route route = events[0].Route;
        for (var index = 1; index < events.Length; index++)
        {
            if (events[index].Route != route)
                return;
        }

        if (route.PartitionId is not null)
            activity.SetTag(nameof(EventHubSendContext.PartitionId), route.PartitionId);
        if (route.PartitionKey is not null)
            activity.SetTag(nameof(PartitionKeySendContext.PartitionKey), route.PartitionKey);
    }

    static EventHubMessageSendContext<T>[] ValidateContexts<T>(EventHubSendContext<T>[] sendContexts)
        where T : class
    {
        var contexts = new EventHubMessageSendContext<T>[sendContexts.Length];

        for (var index = 0; index < sendContexts.Length; index++)
        {
            contexts[index] = sendContexts[index] as EventHubMessageSendContext<T>
                ?? throw new ArgumentException(
                    $"Send context at index {index} is not an EventHubMessageSendContext<{typeof(T).Name}>.",
                    nameof(sendContexts));

            _ = CreateRoute(contexts[index], index);
        }

        return contexts;
    }

    readonly record struct PreparedEvent(EventData EventData, Route Route);

    readonly record struct Route(string? PartitionId, string? PartitionKey);
}
