using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Azure.Messaging.EventHubs;
using Azure.Messaging.EventHubs.Producer;
using ViciOne.ServiceBus.Configuration;
using ViciOne.ServiceBus.Transports;

namespace ViciOne.ServiceBus.EventHubs;

/// <summary>
/// Provides an event hub producer send transport context implementation.
/// </summary>
public class EventHubProducerSendTransportContext :
    BaseSendTransportContext,
    EventHubSendTransportContext
{
    readonly IHostConfiguration _configuration;
    readonly EventHubEndpointAddress _endpointAddress;
    readonly ISendPipe _sendPipe;
    readonly IProducerContextSupervisor _supervisor;

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="supervisor">The supervisor value.</param>
    /// <param name="sendPipe">The send pipe value.</param>
    /// <param name="configuration">The configuration callback.</param>
    /// <param name="eventHubName">The event hub name value.</param>
    /// <param name="serialization">The serialization value.</param>
    public EventHubProducerSendTransportContext(IProducerContextSupervisor supervisor, ISendPipe sendPipe,
        IHostConfiguration configuration, string eventHubName, ISerialization serialization)
        : base(configuration, serialization)
    {
        _sendPipe = sendPipe;
        _supervisor = supervisor;
        _configuration = configuration;
        _endpointAddress = new EventHubEndpointAddress(configuration.HostAddress, eventHubName);
    }

    /// <summary>
    /// Gets agent handles.
    /// </summary>
    /// <returns>The result of the operation.</returns>
    public override IEnumerable<IAgent> GetAgentHandles()
    {
        return [_supervisor];
    }

    /// <summary>
    /// Creates context.
    /// </summary>
    /// <typeparam name="T">The t type.</typeparam>
    /// <param name="value">The value.</param>
    /// <param name="pipe">The pipe value.</param>
    /// <param name="initializerPipe">The initializer pipe value.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The result of the operation.</returns>
    public async Task<EventHubSendContext<T>> CreateContextAsync<T>(T value, IPipe<EventHubSendContext<T>> pipe,
        IPipe<SendContext<T>>? initializerPipe = null, CancellationToken cancellationToken = default)
        where T : class
    {
        var context = new EventHubMessageSendContext<T>(value, cancellationToken)
        {
            Serializer = Serialization.GetMessageSerializer(),
            DestinationAddress = _endpointAddress
        };

        if (pipe is ISendContextPipe sendPipe)
            await sendPipe.SendAsync(context, cancellationToken: cancellationToken).ConfigureAwait(false);

        await _sendPipe.SendAsync(context, cancellationToken: cancellationToken).ConfigureAwait(false);

        if (initializerPipe != null && initializerPipe.IsNotEmpty())
            await initializerPipe.SendAsync(context).ConfigureAwait(false);

        if (pipe.IsNotEmpty())
            await pipe.SendAsync(context).ConfigureAwait(false);

        context.SourceAddress ??= _configuration.HostAddress;

        return context;
    }

    /// <summary>
    /// Sends a message to the configured destination.
    /// </summary>
    /// <typeparam name="T">The t type.</typeparam>
    /// <param name="producerContext">The producer context value.</param>
    /// <param name="sendContext">The send context value.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The result of the operation.</returns>
    public Task SendAsync<T>(ProducerContext producerContext, EventHubSendContext<T> sendContext, CancellationToken cancellationToken = default)
        where T : class
    {
        if (cancellationToken.IsCancellationRequested) return global::System.Threading.Tasks.Task.FromCanceled(cancellationToken); EventHubMessageSendContext<T> context = sendContext as EventHubMessageSendContext<T>
                    ?? throw new ArgumentException("Invalid SendContext<T> type", nameof(sendContext));

        context.ConversationId ??= NewId.NextGuid();

        var options = new SendEventOptions
        {
            PartitionId = context.PartitionId,
            PartitionKey = context.PartitionKey
        };

        if (Activity.Current?.IsAllDataRequested ?? false)
        {
            if (!string.IsNullOrEmpty(options.PartitionId))
                Activity.Current.SetTag(nameof(context.PartitionId), options.PartitionId);
            if (!string.IsNullOrEmpty(options.PartitionKey))
                Activity.Current.SetTag(nameof(context.PartitionKey), options.PartitionKey);
        }

        var eventData = new EventData(context.Body.GetBytes());

        eventData.Properties.Set(context.Headers);

        if (context.MessageId.HasValue)
            eventData.MessageId = context.MessageId.Value.ToString("N");

        if (context.CorrelationId.HasValue)
            eventData.CorrelationId = context.CorrelationId.Value.ToString("N");

        eventData.ContentType = (context.ContentType
            ?? throw new InvalidOperationException("A content type is required before an Event Hub message can be sent.")).ToString();

        context.CancellationToken.ThrowIfCancellationRequested();

        return producerContext.ProduceAsync([eventData], options, context.CancellationToken);
    }

    /// <summary>
    /// Sends a message to the configured destination.
    /// </summary>
    /// <typeparam name="T">The t type.</typeparam>
    /// <param name="producerContext">The producer context value.</param>
    /// <param name="sendContexts">The send contexts value.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The result of the operation.</returns>
    public async Task SendAsync<T>(ProducerContext producerContext, EventHubSendContext<T>[] sendContexts, CancellationToken cancellationToken = default)
        where T : class
    {
        cancellationToken.ThrowIfCancellationRequested(); EventHubSendContext<T> sendContext = sendContexts[0];
        var options = new CreateBatchOptions
        {
            PartitionId = sendContext.PartitionId,
            PartitionKey = sendContext.PartitionKey
        };

        if (Activity.Current?.IsAllDataRequested ?? false)
        {
            Activity.Current.SetTag(nameof(sendContext.PartitionId), options.PartitionId);
            Activity.Current.SetTag(nameof(sendContext.PartitionKey), options.PartitionKey);
        }

        sendContext.CancellationToken.ThrowIfCancellationRequested();

        var eventDataBatch = await producerContext.CreateBatchAsync(options, sendContext.CancellationToken).ConfigureAwait(false);

        async Task FlushAsync(EventDataBatch batch)
        {
            try
            {
                sendContext.CancellationToken.ThrowIfCancellationRequested();

                await producerContext.ProduceAsync(batch, sendContext.CancellationToken).ConfigureAwait(false);
            }
            finally
            {
                batch.Dispose();
            }
        }

        NewId[] ids = NewId.Next(sendContexts.Length);

        for (var i = 0; i < sendContexts.Length; i++)
        {
            EventHubMessageSendContext<T> context = sendContexts[i] as EventHubMessageSendContext<T>
                ?? throw new ArgumentException("Invalid SendContext<T> type", nameof(sendContext));

            context.ConversationId ??= ids[i].ToGuid();

            var eventData = new EventData(context.Body.GetBytes());

            if (context.MessageId.HasValue)
                eventData.MessageId = context.MessageId.Value.ToString("N");

            if (context.CorrelationId.HasValue)
                eventData.CorrelationId = context.CorrelationId.Value.ToString("N");

            eventData.ContentType = (context.ContentType
                ?? throw new InvalidOperationException("A content type is required before an Event Hub message can be sent.")).ToString();

            eventData.Properties.Set(context.Headers);

            if (eventDataBatch.TryAdd(eventData))
                continue;

            await FlushAsync(eventDataBatch).ConfigureAwait(false);
            eventDataBatch = await producerContext.CreateBatchAsync(options, context.CancellationToken).ConfigureAwait(false);

            if (!eventDataBatch.TryAdd(eventData))
                throw new ApplicationException("Message can not be added to the empty EventDataBatch");
        }

        if (eventDataBatch.Count > 0)
            await FlushAsync(eventDataBatch).ConfigureAwait(false);
    }

    /// <summary>
    /// Sends a message to the configured destination.
    /// </summary>
    /// <param name="pipe">The pipe value.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The result of the operation.</returns>
    public Task SendAsync(IPipe<ProducerContext> pipe, CancellationToken cancellationToken)
    {
        return _configuration.RetryAsync(() => _supervisor.SendAsync(pipe, cancellationToken),
            stoppingToken: _supervisor.SendStopping, cancellationToken: cancellationToken);
    }

    /// <summary>
    /// Gets the entity name value.
    /// </summary>
    public override string EntityName => _endpointAddress.EventHubName;
    /// <summary>
    /// Gets the activity system value.
    /// </summary>
    public override string ActivitySystem => "eventhubs";

    /// <summary>
    /// Creates send context.
    /// </summary>
    /// <typeparam name="T">The t type.</typeparam>
    /// <param name="message">The message value.</param>
    /// <param name="pipe">The pipe value.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The result of the operation.</returns>
    public override Task<SendContext<T>> CreateSendContextAsync<T>(T message, IPipe<SendContext<T>> pipe, CancellationToken cancellationToken = default)
    {
        if (cancellationToken.IsCancellationRequested) return global::System.Threading.Tasks.Task.FromCanceled<global::ViciOne.ServiceBus.Advanced.SendContext<T>>(cancellationToken); throw new NotImplementedByDesignException("Event Hub is a producer, not an outbox compatible transport");
    }

    /// <summary>
    /// Performs the probe operation.
    /// </summary>
    /// <param name="context">The operation context.</param>
    public void Probe(ProbeContext context)
    {
        _supervisor.Probe(context);
    }
}
