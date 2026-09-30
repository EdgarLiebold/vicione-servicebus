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

/// <summary>Creates and sends Apache NMS messages for one ActiveMQ destination.</summary>
public class ActiveMqSendTransportContext :
    BaseSendTransportContext,
    SendTransportContext<SessionContext>
{
    readonly IPipe<SessionContext> _configureTopologyPipe;
    readonly DestinationType _destinationType;
    readonly IActiveMqHostConfiguration _hostConfiguration;
    readonly ISessionContextSupervisor _supervisor;

    /// <summary>Creates a send-transport context for an ActiveMQ entity.</summary>
    /// <param name="hostConfiguration">The ActiveMQ host configuration.</param>
    /// <param name="receiveEndpointContext">The endpoint context supplying serialization settings.</param>
    /// <param name="supervisor">The session supervisor used for sends.</param>
    /// <param name="configureTopologyPipe">The pipeline that provisions required broker topology.</param>
    /// <param name="entityName">The destination entity name.</param>
    /// <param name="destinationType">The Apache NMS destination type.</param>
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

    /// <summary>Gets the destination entity name.</summary>
    public override string EntityName { get; }
    /// <summary>Gets the OpenTelemetry messaging-system identifier.</summary>
    public override string ActivitySystem => "activemq";

    /// <summary>Executes a session pipeline with the configured host retry policy.</summary>
    /// <param name="pipe">The session pipeline to execute.</param>
    /// <param name="cancellationToken">The token used to cancel retries and sending.</param>
    /// <returns>A task that completes when the pipeline has completed.</returns>
    public Task SendAsync(IPipe<SessionContext> pipe, CancellationToken cancellationToken = default)
    {
        return _hostConfiguration.RetryAsync(() => _supervisor.SendAsync(pipe, cancellationToken),
            stoppingToken: _supervisor.SendStopping, cancellationToken: cancellationToken);
    }

    /// <summary>Writes diagnostic information to the probe context.</summary>
    /// <param name="context">The probe context to populate.</param>
    public void Probe(ProbeContext context)
    {
        _supervisor.Probe(context);
    }

    /// <summary>Creates and configures an ActiveMQ send context for a message.</summary>
    /// <typeparam name="T">The message type.</typeparam>
    /// <param name="message">The message to send.</param>
    /// <param name="pipe">The pipeline that configures the send context.</param>
    /// <param name="cancellationToken">The token associated with the send.</param>
    /// <returns>A task that produces the configured send context.</returns>
    public override async Task<SendContext<T>> CreateSendContextAsync<T>(T message, IPipe<SendContext<T>> pipe, CancellationToken cancellationToken = default)
    {
        var sendContext = new TransportActiveMqSendContext<T>(message, cancellationToken);

        await pipe.SendAsync(sendContext).ConfigureAwait(false);

        return sendContext;
    }

    /// <summary>Gets the session supervisor used by this transport.</summary>
    /// <returns>The transport's supervised session agent.</returns>
    public override IEnumerable<IAgent> GetAgentHandles()
    {
        return new IAgent[] { _supervisor };
    }

    /// <summary>Creates and configures an ActiveMQ send context within a session pipeline.</summary>
    /// <typeparam name="T">The message type.</typeparam>
    /// <param name="sessionContext">The active session context; creation itself does not require native session state.</param>
    /// <param name="message">The message to send.</param>
    /// <param name="pipe">The pipeline that configures the send context.</param>
    /// <param name="cancellationToken">The token associated with the send.</param>
    /// <returns>A task that produces the configured send context.</returns>
    public Task<SendContext<T>> CreateSendContextAsync<T>(SessionContext sessionContext, T message, IPipe<SendContext<T>> pipe,
        CancellationToken cancellationToken)
        where T : class
    {
        return CreateSendContextAsync(message, pipe, cancellationToken);
    }

    /// <summary>Serializes and sends a message through an Apache NMS session.</summary>
    /// <typeparam name="T">The message type.</typeparam>
    /// <param name="sessionContext">The native session context.</param>
    /// <param name="sendContext">The configured ActiveMQ send context.</param>
    /// <param name="cancellationToken">The token used while resolving the destination.</param>
    /// <returns>A task that completes when the native send completes.</returns>
    public async Task SendAsync<T>(SessionContext sessionContext, SendContext<T> sendContext, CancellationToken cancellationToken = default)
        where T : class
    {
        TransportActiveMqSendContext<T> context = sendContext as TransportActiveMqSendContext<T>
            ?? throw new ArgumentException("Invalid SendContext<T> type", nameof(sendContext));

        sendContext.CancellationToken.ThrowIfCancellationRequested();

        await _configureTopologyPipe.SendAsync(sessionContext).ConfigureAwait(false);

        sendContext.CancellationToken.ThrowIfCancellationRequested();

        var destination = context.ReplyDestination ?? await sessionContext.GetDestinationAsync(EntityName, _destinationType, cancellationToken: cancellationToken).ConfigureAwait(false);

        IDestination? replyDestinationBeforeBody = context.ReplyDestination;
        byte[] body;
        try
        {
            body = TransportBodyMaterializer.ToArray(context);
        }
        catch (Exception failure)
        {
            if (!ReferenceEquals(context.ReplyDestination, replyDestinationBeforeBody))
            {
                context.ReplyDestination = replyDestinationBeforeBody;
                TransportBodyMaterializer.MarkMutationFailure(failure);
            }
            throw;
        }
        if (!ReferenceEquals(context.ReplyDestination, replyDestinationBeforeBody))
        {
            context.ReplyDestination = replyDestinationBeforeBody;
            throw TransportBodyMaterializer.CreateMutationFailure<T>(nameof(context.ReplyDestination));
        }

        var transportMessage = sessionContext.CreateBytesMessage(body);

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

        transportMessage.NMSReplyTo = sessionContext.GetTemporaryDestination(endpointName, DestinationType.TemporaryQueue)
            ?? (context.ResponseAddress.TryGetValueFromQueryString("temporary", out _)
                ? await sessionContext.GetDestinationAsync(endpointName, DestinationType.TemporaryQueue)
                : await sessionContext.GetDestinationAsync(endpointName, DestinationType.Queue));
    }
}
