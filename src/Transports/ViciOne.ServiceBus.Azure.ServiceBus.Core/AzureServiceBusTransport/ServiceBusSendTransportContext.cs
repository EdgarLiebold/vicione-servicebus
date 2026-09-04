using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Azure.Messaging.ServiceBus;
using ViciOne.ServiceBus.AzureServiceBusTransport.Configuration;
using ViciOne.ServiceBus.Scheduling;
using ViciOne.ServiceBus.Transports;

namespace ViciOne.ServiceBus.AzureServiceBusTransport;

public class ServiceBusSendTransportContext :
    BaseSendTransportContext,
    SendTransportContext<SendEndpointContext>
{
    internal static readonly ITransportSetHeaderAdapter<object> Adapter =
        new DictionaryTransportSetHeaderAdapter(new SimpleHeaderValueConverter()) { MaxHeaderLength = Defaults.MaxHeaderLength };

    readonly IServiceBusHostConfiguration _hostConfiguration;
    readonly ISendEndpointContextSupervisor _supervisor;

    public ServiceBusSendTransportContext(IServiceBusHostConfiguration hostConfiguration, ReceiveEndpointContext receiveEndpointContext,
        ISendEndpointContextSupervisor supervisor, SendSettings settings)
        : base(hostConfiguration, receiveEndpointContext.Serialization)
    {
        _hostConfiguration = hostConfiguration;
        _supervisor = supervisor;

        EntityName = settings.EntityPath;
    }

    public override string EntityName { get; }
    public override string ActivitySystem => "servicebus";

    public Task SendAsync(IPipe<SendEndpointContext> pipe, CancellationToken cancellationToken = default)
    {
        return _hostConfiguration.RetryAsync(() => _supervisor.SendAsync(pipe, cancellationToken),
            stoppingToken: _supervisor.SendStopping, cancellationToken: cancellationToken);
    }

    public void Probe(ProbeContext context)
    {
        _supervisor.Probe(context);
    }

    public override async Task<SendContext<T>> CreateSendContextAsync<T>(T message, IPipe<SendContext<T>> pipe, CancellationToken cancellationToken = default)
    {
        var sendContext = new AzureServiceBusSendContext<T>(message, cancellationToken);

        await pipe.SendAsync(sendContext).ConfigureAwait(false);

        CopyIncomingIdentifiersIfPresent(sendContext);

        return sendContext;
    }

    public override IEnumerable<IAgent> GetAgentHandles()
    {
        return [_supervisor];
    }

    public Task<SendContext<T>> CreateSendContextAsync<T>(SendEndpointContext context, T message, IPipe<SendContext<T>> pipe,
        CancellationToken cancellationToken)
        where T : class
    {
        return CreateSendContextAsync(message, pipe, cancellationToken);
    }

    public async Task SendAsync<T>(SendEndpointContext sendEndpointContext, SendContext<T> sendContext, CancellationToken cancellationToken = default)
        where T : class
    {
        cancellationToken.ThrowIfCancellationRequested(); AzureServiceBusSendContext<T> context = sendContext as AzureServiceBusSendContext<T>
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
            ViciOne.ServiceBus.LogContext.Debug?.Log("The scheduled time was in the past, sending: {DueAt}", context.ScheduledEnqueueTimeUtc);

            return false;
        }

        try
        {
            context.Headers.Set(MessageHeaders.SchedulingTokenId, null);

            var message = CreateMessage(context);

            var sequenceNumber = await clientContext.ScheduleSendAsync(message, enqueueTimeUtc.UtcDateTime, context.CancellationToken).ConfigureAwait(false);

            context.SetScheduledMessageId(sequenceNumber);

            context.LogScheduled(enqueueTimeUtc.UtcDateTime);

            return true;
        }
        catch (ArgumentOutOfRangeException)
        {
            ViciOne.ServiceBus.LogContext.Debug?.Log("The scheduled time was rejected by the server, sending: {MessageId}", context.MessageId);

            return false;
        }
    }

    async Task CancelScheduledSendAsync(SendEndpointContext clientContext, Guid tokenId, long sequenceNumber, CancellationToken cancellationToken)
    {
        try
        {
            await clientContext.CancelScheduledSendAsync(sequenceNumber, cancellationToken).ConfigureAwait(false);

            ViciOne.ServiceBus.LogContext.Debug?.Log("CANCEL {DestinationAddress} {TokenId}", EntityName, tokenId);
        }
        catch (ServiceBusException exception) when (exception.Reason == ServiceBusFailureReason.MessageNotFound)
        {
            ViciOne.ServiceBus.LogContext.Debug?.Log("CANCEL {DestinationAddress} {TokenId} message not found", EntityName, tokenId);
        }
        catch (InvalidOperationException exception) when (exception.Message.Contains("already being cancelled"))
        {
            ViciOne.ServiceBus.LogContext.Debug?.Log("CANCEL {DestinationAddress} {TokenId} message already being canceled", EntityName, tokenId);
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
        var message = new ServiceBusMessage(context.Body.GetBytes())
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
            && consumeContext.TryGetPayload<ServiceBusMessageContext>(out var brokeredMessageContext))
        {
            if (context.SessionId == null)
            {
                if (brokeredMessageContext.ReplyToSessionId != null)
                    context.SessionId = brokeredMessageContext.ReplyToSessionId;
                else if (brokeredMessageContext.SessionId != null)
                    context.SessionId = brokeredMessageContext.SessionId;
            }

            if (context.PartitionKey == null && brokeredMessageContext.PartitionKey != null)
                context.PartitionKey = brokeredMessageContext.PartitionKey;
        }
    }
}
