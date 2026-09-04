using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Apache.NMS;
using Apache.NMS.ActiveMQ;
using ViciOne.ServiceBus.ActiveMq.Configuration;
using ViciOne.ServiceBus.Internals;
using ViciOne.ServiceBus.Transports;

namespace ViciOne.ServiceBus.ActiveMq;

/// <summary>
/// Provides an active mq send transport context implementation.
/// </summary>
public class ActiveMqSendTransportContext :
    BaseSendTransportContext,
    SendTransportContext<SessionContext>
{
    readonly IPipe<SessionContext> _configureTopologyPipe;
    readonly DestinationType _destinationType;
    readonly IActiveMqHostConfiguration _hostConfiguration;
    readonly ISessionContextSupervisor _supervisor;

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="hostConfiguration">The host configuration value.</param>
    /// <param name="receiveEndpointContext">The receive endpoint context value.</param>
    /// <param name="supervisor">The supervisor value.</param>
    /// <param name="configureTopologyPipe">The configure topology pipe value.</param>
    /// <param name="entityName">The entity name value.</param>
    /// <param name="destinationType">The destination type value.</param>
    public ActiveMqSendTransportContext(IActiveMqHostConfiguration hostConfiguration, ReceiveEndpointContext receiveEndpointContext,
        ISessionContextSupervisor supervisor, IPipe<SessionContext> configureTopologyPipe, string entityName, DestinationType destinationType)
        : base(hostConfiguration, receiveEndpointContext.Serialization)
    {
        _hostConfiguration = hostConfiguration;
        _supervisor = supervisor;
        _configureTopologyPipe = configureTopologyPipe;
        _destinationType = destinationType;

        EntityName = entityName;
    }

    /// <summary>
    /// Gets the entity name value.
    /// </summary>
    public override string EntityName { get; }
    /// <summary>
    /// Gets the activity system value.
    /// </summary>
    public override string ActivitySystem => "activemq";

    /// <summary>
    /// Sends a message to the configured destination.
    /// </summary>
    /// <param name="pipe">The pipe value.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The result of the operation.</returns>
    public Task SendAsync(IPipe<SessionContext> pipe, CancellationToken cancellationToken = default)
    {
        return _hostConfiguration.RetryAsync(() => _supervisor.SendAsync(pipe, cancellationToken),
            stoppingToken: _supervisor.SendStopping, cancellationToken: cancellationToken);
    }

    /// <summary>
    /// Performs the probe operation.
    /// </summary>
    /// <param name="context">The operation context.</param>
    public void Probe(ProbeContext context)
    {
        _supervisor.Probe(context);
    }

    /// <summary>
    /// Creates send context.
    /// </summary>
    /// <typeparam name="T">The t type.</typeparam>
    /// <param name="message">The message value.</param>
    /// <param name="pipe">The pipe value.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The result of the operation.</returns>
    public override async Task<SendContext<T>> CreateSendContextAsync<T>(T message, IPipe<SendContext<T>> pipe, CancellationToken cancellationToken = default)
    {
        var sendContext = new TransportActiveMqSendContext<T>(message, cancellationToken);

        await pipe.SendAsync(sendContext).ConfigureAwait(false);

        return sendContext;
    }

    /// <summary>
    /// Gets agent handles.
    /// </summary>
    /// <returns>The result of the operation.</returns>
    public override IEnumerable<IAgent> GetAgentHandles()
    {
        return new IAgent[] { _supervisor };
    }

    /// <summary>
    /// Creates send context.
    /// </summary>
    /// <typeparam name="T">The t type.</typeparam>
    /// <param name="sessionContext">The session context value.</param>
    /// <param name="message">The message value.</param>
    /// <param name="pipe">The pipe value.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The result of the operation.</returns>
    public Task<SendContext<T>> CreateSendContextAsync<T>(SessionContext sessionContext, T message, IPipe<SendContext<T>> pipe,
        CancellationToken cancellationToken)
        where T : class
    {
        return CreateSendContextAsync(message, pipe, cancellationToken);
    }

    /// <summary>
    /// Sends a message to the configured destination.
    /// </summary>
    /// <typeparam name="T">The t type.</typeparam>
    /// <param name="sessionContext">The session context value.</param>
    /// <param name="sendContext">The send context value.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The result of the operation.</returns>
    public async Task SendAsync<T>(SessionContext sessionContext, SendContext<T> sendContext, CancellationToken cancellationToken = default)
        where T : class
    {
        TransportActiveMqSendContext<T> context = sendContext as TransportActiveMqSendContext<T>
            ?? throw new ArgumentException("Invalid SendContext<T> type", nameof(sendContext));

        sendContext.CancellationToken.ThrowIfCancellationRequested();

        await _configureTopologyPipe.SendAsync(sessionContext).ConfigureAwait(false);

        sendContext.CancellationToken.ThrowIfCancellationRequested();

        var destination = context.ReplyDestination ?? await sessionContext.GetDestinationAsync(EntityName, _destinationType, cancellationToken: cancellationToken).ConfigureAwait(false);

        var transportMessage = sessionContext.CreateBytesMessage(context.Body.GetBytes());

        await SetResponseToAsync(transportMessage, context, sessionContext);

        transportMessage.Properties.SetHeaders(context.Headers);

        transportMessage.Properties[MessageHeaders.ContentType] = (context.ContentType
            ?? throw new InvalidOperationException("A content type is required before an ActiveMQ message can be sent.")).ToString();

        if (context.MessageId.HasValue)
        {
            // NMSMessageId is broker-owned and both Classic ActiveMQ transports replace a
            // caller-assigned value with their provider identity. Preserve the service-bus
            // message identity independently so receive-fault generation can still correlate
            // a body that is too damaged to yield its envelope metadata.
            transportMessage.Properties[MessageHeaders.MessageId] = context.MessageId.Value.ToString("D");
            transportMessage.NMSMessageId = context.MessageId.ToString();
        }

        if (context.CorrelationId.HasValue)
            transportMessage.NMSCorrelationID = context.CorrelationId.ToString();

        transportMessage.NMSDeliveryMode = context.Durable ? MsgDeliveryMode.Persistent : MsgDeliveryMode.NonPersistent;

        ApplyTimeToLive(transportMessage, context, sessionContext.Session is Session);

        transportMessage.NMSPriority = context.Priority ?? NMSConstants.defaultPriority;

        if (!string.IsNullOrWhiteSpace(context.GroupId))
            transportMessage.SetGroupId(context.GroupId);

        if (context.GroupSequence.HasValue)
            transportMessage.SetGroupSequence(context.GroupSequence.Value);

        ApplyDeliveryDelay(transportMessage, context, _hostConfiguration.IsArtemis);

        await sessionContext.SendAsync(destination, transportMessage, context.CancellationToken).ConfigureAwait(false);
    }

    internal static void ApplyTimeToLive(IMessage transportMessage, SendContext context, bool useOpenWireDefault)
    {
        if (context.TimeToLive.HasValue)
        {
            if (context.TimeToLive.Value <= TimeSpan.Zero)
                throw new ArgumentOutOfRangeException(nameof(context), "Expired messages must be discarded before transport serialization.");

            transportMessage.NMSTimeToLive = context.TimeToLive.Value;
        }
        else if (useOpenWireDefault)
            transportMessage.NMSTimeToLive = NMSConstants.defaultTimeToLive;
    }

    internal static void ApplyDeliveryDelay(IMessage transportMessage, SendContext context, bool isArtemis)
    {
        ArgumentNullException.ThrowIfNull(transportMessage);
        ArgumentNullException.ThrowIfNull(context);

        if (!context.Delay.HasValue || context.Delay.Value <= TimeSpan.Zero)
            return;

        if (isArtemis)
        {
            // Apache.NMS.AMQP maps NMSDeliveryTime to the AMQP x-opt-delivery-time
            // annotation understood by Artemis. A regular application property named
            // _AMQ_SCHED_DELIVERY is only the Core/JMS contract and is ignored on AMQP.
            transportMessage.NMSDeliveryTime = (context.GetTimeProvider().GetUtcNow() + context.Delay.Value).UtcDateTime;
        }
        else
            transportMessage.Properties["AMQ_SCHEDULED_DELAY"] = checked((long)context.Delay.Value.TotalMilliseconds);
    }

    static async Task SetResponseToAsync(IMessage transportMessage, SendContext context, SessionContext sessionContext)
    {
        if (context.ResponseAddress == null)
            return;

        var endpointName = context.ResponseAddress.GetEndpointName();
        if (string.IsNullOrWhiteSpace(endpointName))
            throw new InvalidOperationException("The response address must contain an endpoint name.");

        transportMessage.NMSReplyTo = sessionContext.GetTemporaryDestination(endpointName)
            ?? (context.ResponseAddress.TryGetValueFromQueryString("temporary", out _)
                ? await sessionContext.GetDestinationAsync(endpointName, DestinationType.TemporaryQueue)
                : await sessionContext.GetDestinationAsync(endpointName, DestinationType.Queue));
    }
}
