using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Azure.Messaging.ServiceBus;
using ViciOne.ServiceBus.AzureServiceBus.Configuration;
using ViciOne.ServiceBus.Scheduling;
using ViciOne.ServiceBus.Transports;

namespace ViciOne.ServiceBus.AzureServiceBus;

/// <summary>Creates Azure Service Bus messages and sends or schedules them through a supervised sender.</summary>
public class ServiceBusSendTransportContext :
    BaseSendTransportContext,
    SendTransportContext<SendEndpointContext>
{
    internal static readonly ITransportSetHeaderAdapter<object> Adapter =
        new DictionaryTransportSetHeaderAdapter(new SimpleHeaderValueConverter()) { MaxHeaderLength = Defaults.MaxHeaderLength };

    readonly IServiceBusHostConfiguration _hostConfiguration;
    readonly ISendEndpointContextSupervisor _supervisor;

    /// <summary>Creates a send transport for one Azure Service Bus entity.</summary>
    /// <param name="hostConfiguration">The namespace connection and retry configuration.</param>
    /// <param name="receiveEndpointContext">The receive endpoint providing serialization settings.</param>
    /// <param name="supervisor">The sender-context supervisor.</param>
    /// <param name="settings">The destination entity and sender settings.</param>
    public ServiceBusSendTransportContext(IServiceBusHostConfiguration hostConfiguration, ReceiveEndpointContext receiveEndpointContext,
        ISendEndpointContextSupervisor supervisor, SendSettings settings)
        : base(hostConfiguration, receiveEndpointContext.Serialization)
    {
        _hostConfiguration = hostConfiguration;
        _supervisor = supervisor;

        EntityName = settings.EntityPath;
    }

    /// <summary>Gets the namespace-relative destination entity path.</summary>
    public override string EntityName { get; }
    /// <summary>Gets the OpenTelemetry messaging-system identifier.</summary>
    public override string ActivitySystem => "servicebus";

    /// <summary>Executes a sender-context pipe under the configured retry policy.</summary>
    /// <param name="pipe">The operations to execute with the sender context.</param>
    /// <param name="cancellationToken">Cancels retries and sender acquisition.</param>
    /// <returns>The retry-wrapped supervisor task for <paramref name="pipe"/>.</returns>
    public Task SendAsync(IPipe<SendEndpointContext> pipe, CancellationToken cancellationToken = default)
    {
        return _hostConfiguration.RetryAsync(() => _supervisor.SendAsync(pipe, cancellationToken),
            stoppingToken: _supervisor.SendStopping, cancellationToken: cancellationToken);
    }

    /// <summary>Adds sender-supervisor diagnostics to a probe.</summary>
    /// <param name="context">The probe receiving diagnostic values.</param>
    public void Probe(ProbeContext context)
    {
        _supervisor.Probe(context);
    }

    /// <summary>Creates an Azure send context, applies the send pipe, and inherits applicable incoming identifiers.</summary>
    /// <typeparam name="T">The message contract.</typeparam>
    /// <param name="message">The message instance to send.</param>
    /// <param name="pipe">The send-context configuration pipe.</param>
    /// <param name="cancellationToken">Cancels send-context configuration.</param>
    /// <returns>A task that produces the configured send context.</returns>
    public override async Task<SendContext<T>> CreateSendContextAsync<T>(T message, IPipe<SendContext<T>> pipe, CancellationToken cancellationToken = default)
    {
        var sendContext = new AzureServiceBusSendContext<T>(message, cancellationToken);

        await pipe.SendAsync(sendContext).ConfigureAwait(false);

        CopyIncomingIdentifiersIfPresent(sendContext);

        return sendContext;
    }

    /// <summary>Gets the sender supervisor owned by this transport.</summary>
    /// <returns>The transport agent handles.</returns>
    public override IEnumerable<IAgent> GetAgentHandles()
    {
        return [_supervisor];
    }

    /// <summary>Creates a configured Azure send context for a sender-context pipeline.</summary>
    /// <typeparam name="T">The message contract.</typeparam>
    /// <param name="context">The acquired sender context; context creation does not use it.</param>
    /// <param name="message">The message instance to send.</param>
    /// <param name="pipe">The send-context configuration pipe.</param>
    /// <param name="cancellationToken">Cancels send-context configuration.</param>
    /// <returns>A task that produces the configured send context.</returns>
    public Task<SendContext<T>> CreateSendContextAsync<T>(SendEndpointContext context, T message, IPipe<SendContext<T>> pipe,
        CancellationToken cancellationToken)
        where T : class
    {
        return CreateSendContextAsync(message, pipe, cancellationToken);
    }

    /// <summary>Sends, schedules, or cancels a scheduled Azure Service Bus message.</summary>
    /// <typeparam name="T">The message contract.</typeparam>
    /// <param name="sendEndpointContext">The acquired Azure sender context.</param>
    /// <param name="sendContext">The configured outgoing message context.</param>
    /// <param name="cancellationToken">Cancels before the send begins; the send context token governs broker calls.</param>
    /// <returns>A task that completes after the selected broker operation.</returns>
    public async Task SendAsync<T>(SendEndpointContext sendEndpointContext, SendContext<T> sendContext, CancellationToken cancellationToken = default)
        where T : class
    {
        cancellationToken.ThrowIfCancellationRequested();

        AzureServiceBusSendContext<T> context = sendContext as AzureServiceBusSendContext<T>
            ?? throw new ArgumentException("Invalid SendContext<T> type", nameof(sendContext));

        if (Activity.Current?.IsAllDataRequested ?? false)
        {
            if (!string.IsNullOrWhiteSpace(context.PartitionKey))
                Activity.Current.SetTag(nameof(context.PartitionKey), context.PartitionKey);
            if (!string.IsNullOrWhiteSpace(context.SessionId))
                Activity.Current.SetTag(nameof(context.SessionId), context.SessionId);
        }

        sendContext.CancellationToken.ThrowIfCancellationRequested();

        if (IsCancelScheduledSend(context, out var tokenId, out var sequenceNumber))
        {
            await CancelScheduledSendAsync(sendEndpointContext, tokenId, sequenceNumber, sendContext.CancellationToken).ConfigureAwait(false);

            return;
        }

        if (context.ScheduledEnqueueTimeUtc.HasValue)
        {
            var scheduled = await ScheduleSendAsync(sendEndpointContext, context).ConfigureAwait(false);
            if (scheduled)
                return;
        }

        var message = CreateMessage(context);

        await sendEndpointContext.SendAsync(message, context.CancellationToken).ConfigureAwait(false);
    }

    static async Task<bool> ScheduleSendAsync<T>(SendEndpointContext clientContext, AzureServiceBusSendContext<T> context)
        where T : class
    {
        DateTimeOffset now = context.GetTimeProvider().GetUtcNow();

        DateTimeOffset enqueueTimeUtc = context.ScheduledEnqueueTimeUtc
            ?? throw new InvalidOperationException("A scheduled enqueue time is required for a scheduled send.");
        if (enqueueTimeUtc < now)
        {
            try
            {
                ViciOne.ServiceBus.Advanced.LogContext.Debug?.Log("The scheduled time was in the past, sending: {DueAt}", context.ScheduledEnqueueTimeUtc);
            }
            catch (Exception)
            {
                // Optional diagnostics cannot replace the endpoint or broker operation.
            }

            return false;
        }

        try
        {
            context.Headers.Set(MessageHeaders.SchedulingTokenId, null);

            var message = CreateMessage(context);

            var sequenceNumber = await clientContext.ScheduleSendAsync(message, enqueueTimeUtc.UtcDateTime, context.CancellationToken).ConfigureAwait(false);

            context.SetScheduledMessageId(sequenceNumber);

            try
            {
                context.LogScheduled(enqueueTimeUtc.UtcDateTime);
            }
            catch (Exception)
            {
                // Optional diagnostics cannot replace the endpoint or broker operation.
            }

            return true;
        }
        catch (ArgumentOutOfRangeException)
        {
            try
            {
                ViciOne.ServiceBus.Advanced.LogContext.Debug?.Log("The scheduled time was rejected by the server, sending: {MessageId}", context.MessageId);
            }
            catch (Exception)
            {
                // Optional diagnostics cannot replace the endpoint or broker operation.
            }

            return false;
        }
    }

    async Task CancelScheduledSendAsync(SendEndpointContext clientContext, Guid tokenId, long sequenceNumber, CancellationToken cancellationToken)
    {
        try
        {
            await clientContext.CancelScheduledSendAsync(sequenceNumber, cancellationToken).ConfigureAwait(false);

            try
            {
                ViciOne.ServiceBus.Advanced.LogContext.Debug?.Log("CANCEL {DestinationAddress} {TokenId}", EntityName, tokenId);
            }
            catch (Exception)
            {
                // Optional diagnostics cannot replace the endpoint or broker operation.
            }
        }
        catch (ServiceBusException exception) when (exception.Reason == ServiceBusFailureReason.MessageNotFound)
        {
            try
            {
                ViciOne.ServiceBus.Advanced.LogContext.Debug?.Log("CANCEL {DestinationAddress} {TokenId} message not found", EntityName, tokenId);
            }
            catch (Exception)
            {
                // Optional diagnostics cannot replace the endpoint or broker operation.
            }
        }
        catch (InvalidOperationException exception) when (exception.Message.Contains("already being cancelled"))
        {
            try
            {
                ViciOne.ServiceBus.Advanced.LogContext.Debug?.Log("CANCEL {DestinationAddress} {TokenId} message already being canceled", EntityName, tokenId);
            }
            catch (Exception)
            {
                // Optional diagnostics cannot replace the endpoint or broker operation.
            }
        }
    }

    static bool IsCancelScheduledSend<T>(AzureServiceBusSendContext<T> context, out Guid tokenId, out long sequenceNumber)
        where T : class
    {
        if (context.Message is CancelScheduledMessage cancelScheduledMessage)
        {
            tokenId = cancelScheduledMessage.TokenId;

            if (context.TryGetScheduledMessageId(out sequenceNumber)
                || context.TryGetSequenceNumber(cancelScheduledMessage.TokenId, out sequenceNumber))
                return true;
        }

        tokenId = Guid.Empty;
        sequenceNumber = 0;
        return false;
    }

    static ServiceBusMessage CreateMessage<T>(AzureServiceBusSendContext<T> context)
        where T : class
    {
        var message = new ServiceBusMessage(BinaryData.FromBytes(TransportBodyMaterializer.ToArray(context)))
        {
            ContentType = (context.ContentType
                ?? throw new InvalidOperationException("A content type is required before an Azure Service Bus message can be sent.")).ToString()
        };

        Adapter.Set(message.ApplicationProperties, context.Headers);

        if (context.TimeToLive.HasValue)
            message.TimeToLive = context.TimeToLive > TimeSpan.Zero ? context.TimeToLive.Value : TimeSpan.FromSeconds(1);

        if (context.MessageId.HasValue)
            message.MessageId = context.MessageId.Value.ToString("N");

        if (context.CorrelationId.HasValue)
            message.CorrelationId = context.CorrelationId.Value.ToString("N");

        if (context.PartitionKey != null)
            message.PartitionKey = context.PartitionKey;

        if (!string.IsNullOrWhiteSpace(context.SessionId))
        {
            message.SessionId = context.SessionId;

            if (context.ReplyToSessionId == null)
                message.ReplyToSessionId = context.SessionId;
        }

        if (context.ReplyToSessionId != null)
            message.ReplyToSessionId = context.ReplyToSessionId;

        if (context.ReplyTo != null)
            message.ReplyTo = context.ReplyTo;

        if (context.Label != null)
            message.Subject = context.Label;

        return message;
    }

    static void CopyIncomingIdentifiersIfPresent<T>(AzureServiceBusSendContext<T> context)
        where T : class
    {
        if (context.TryGetPayload<ConsumeContext>(out var consumeContext)
            && consumeContext.TryGetPayload<ServiceBusMessageContext>(out var messageContext))
        {
            if (context.SessionId == null)
            {
                if (messageContext.ReplyToSessionId != null)
                    context.SessionId = messageContext.ReplyToSessionId;
                else if (messageContext.SessionId != null)
                    context.SessionId = messageContext.SessionId;
            }

            if (context.PartitionKey == null && messageContext.PartitionKey != null)
                context.PartitionKey = messageContext.PartitionKey;
        }
    }
}
