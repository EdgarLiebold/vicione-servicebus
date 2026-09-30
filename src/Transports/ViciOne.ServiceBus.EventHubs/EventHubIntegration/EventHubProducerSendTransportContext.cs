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

/// <summary>Creates Event Hubs send contexts, serializes them to Azure SDK events, and executes provider sends through a supervised producer.</summary>
public class EventHubProducerSendTransportContext :
    BaseSendTransportContext,
    EventHubSendTransportContext
{
    readonly IHostConfiguration _configuration;
    readonly EventHubEndpointAddress _endpointAddress;
    readonly ISendPipe _sendPipe;
    readonly IProducerContextSupervisor _supervisor;

    /// <summary>Creates a send transport for one Event Hub.</summary>
    /// <param name="supervisor">The producer-context supervisor.</param>
    /// <param name="sendPipe">The rider-level send configuration pipe.</param>
    /// <param name="configuration">The bus host configuration.</param>
    /// <param name="eventHubName">The Event Hub entity name.</param>
    /// <param name="serialization">The serializer collection used for outbound messages.</param>
    public EventHubProducerSendTransportContext(IProducerContextSupervisor supervisor, ISendPipe sendPipe,
        IHostConfiguration configuration, string eventHubName, ISerialization serialization)
        : base(configuration, serialization)
    {
        _sendPipe = sendPipe;
        _supervisor = supervisor;
        _configuration = configuration;
        _endpointAddress = new EventHubEndpointAddress(configuration.HostAddress, eventHubName);
    }

    /// <summary>Gets the producer supervisor owned by senders using this context.</summary>
    /// <returns>A sequence containing the producer supervisor.</returns>
    public override IEnumerable<IAgent> GetAgentHandles()
    {
        return [_supervisor];
    }

    /// <summary>Creates a send context and applies generic, rider-level, initializer, and Event Hubs-specific send pipes in that order.</summary>
    /// <typeparam name="T">The message type.</typeparam>
    /// <param name="value">The outbound message.</param>
    /// <param name="pipe">The Event Hubs-specific context pipe.</param>
    /// <param name="initializerPipe">The optional message initializer pipe.</param>
    /// <param name="cancellationToken">Cancels context configuration.</param>
    /// <returns>A task whose result is the fully configured send context.</returns>
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

    /// <summary>Converts one send context to Azure SDK event data and submits it to the producer.</summary>
    /// <typeparam name="T">The message type.</typeparam>
    /// <param name="producerContext">The active producer context.</param>
    /// <param name="sendContext">The configured outbound message context.</param>
    /// <param name="cancellationToken">Cancels preparation or provider submission.</param>
    /// <returns>A task that completes after the provider accepts the event.</returns>
    public async Task SendAsync<T>(ProducerContext producerContext, EventHubSendContext<T> sendContext,
        CancellationToken cancellationToken = default)
        where T : class
    {
        ArgumentNullException.ThrowIfNull(producerContext);
        EventHubMessageSendContext<T> context = sendContext as EventHubMessageSendContext<T>
            ?? throw new ArgumentException("The context must be an EventHubMessageSendContext<T>.", nameof(sendContext));

        using CancellationTokenSource operationTokenSource =
            CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, context.CancellationToken);
        CancellationToken operationToken = operationTokenSource.Token;
        operationToken.ThrowIfCancellationRequested();

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

        var eventData = new EventData(TransportBodyMaterializer.ToArray(context));

        eventData.Properties.Set(context.Headers);

        if (context.MessageId.HasValue)
            eventData.MessageId = context.MessageId.Value.ToString("N");

        if (context.CorrelationId.HasValue)
            eventData.CorrelationId = context.CorrelationId.Value.ToString("N");

        eventData.ContentType = (context.ContentType
            ?? throw new InvalidOperationException("A content type is required before an Event Hub message can be sent.")).ToString();

        await producerContext.ProduceAsync([eventData], options, operationToken).ConfigureAwait(false);
    }

    /// <summary>Converts send contexts to Azure SDK event data and emits as many size-constrained batches as required.</summary>
    /// <typeparam name="T">The message type.</typeparam>
    /// <param name="producerContext">The active producer context.</param>
    /// <param name="sendContexts">The outbound contexts; each context retains its declared partition route.</param>
    /// <param name="cancellationToken">Cancels validation, batch creation, or provider submission.</param>
    /// <returns>A task that completes after every generated batch has been sent.</returns>
    public Task SendAsync<T>(ProducerContext producerContext, EventHubSendContext<T>[] sendContexts,
        CancellationToken cancellationToken = default)
        where T : class
    {
        return EventHubProducerBatchSender.SendAsync(producerContext, sendContexts, cancellationToken);
    }

    /// <summary>Runs a producer operation through the supervised context and host retry policy.</summary>
    /// <param name="pipe">The operation to execute with an active producer context.</param>
    /// <param name="cancellationToken">Cancels context acquisition, retry, or operation execution.</param>
    /// <returns>The host-retry task that acquires a producer and executes <paramref name="pipe"/>.</returns>
    public Task SendAsync(IPipe<ProducerContext> pipe, CancellationToken cancellationToken)
    {
        return _configuration.RetryAsync(() => _supervisor.SendAsync(pipe, cancellationToken),
            stoppingToken: _supervisor.SendStopping, cancellationToken: cancellationToken);
    }

    /// <summary>Gets the destination Event Hub entity name.</summary>
    public override string EntityName => _endpointAddress.EventHubName;
    /// <summary>Gets the OpenTelemetry messaging-system identifier.</summary>
    public override string ActivitySystem => "eventhubs";

    /// <summary>Rejects generic outbox send-context creation because this producer-only transport requires an Event Hubs context.</summary>
    /// <typeparam name="T">The message type.</typeparam>
    /// <param name="message">The outbound message.</param>
    /// <param name="pipe">The generic send-context pipe.</param>
    /// <param name="cancellationToken">Cancels the call before the unsupported-operation exception is created.</param>
    /// <returns>A canceled task when cancellation was already requested; otherwise, this method throws.</returns>
    public override Task<SendContext<T>> CreateSendContextAsync<T>(T message, IPipe<SendContext<T>> pipe, CancellationToken cancellationToken = default)
    {
        if (cancellationToken.IsCancellationRequested)
            return Task.FromCanceled<SendContext<T>>(cancellationToken);

        throw new NotSupportedException("Event Hubs is a producer-only transport and cannot create an outbox send context.");
    }

    /// <summary>Writes diagnostic information to the probe context.</summary>
    /// <param name="context">The probe context receiving producer-supervisor diagnostics.</param>
    public void Probe(ProbeContext context)
    {
        _supervisor.Probe(context);
    }
}
