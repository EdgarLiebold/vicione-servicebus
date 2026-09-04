using System;
using System.Threading;
using System.Threading.Tasks;
using ViciOne.ServiceBus.Middleware;

namespace ViciOne.ServiceBus.Scheduling;

public class ServiceBusScheduleMessageProvider :
    IScheduleMessageProvider
{
    readonly ISendEndpointProvider _sendEndpointProvider;

    public ServiceBusScheduleMessageProvider(ISendEndpointProvider sendEndpointProvider)
    {
        _sendEndpointProvider = sendEndpointProvider;
    }

    public ServiceBusScheduleMessageProvider(ConsumeContext consumeContext)
    {
        var context = InternalOutboxExtensions.SkipOutbox(consumeContext);

        _sendEndpointProvider = context;
    }

    public async Task<ScheduledMessage<T>> ScheduleSendAsync<T>(Uri destinationAddress, DateTimeOffset dueAt, T message, IPipe<SendContext<T>> pipe,
        CancellationToken cancellationToken)
        where T : class
    {
        if (!MessageTypeCache<T>.IsValidMessageType)
            throw new ArgumentException(MessageTypeCache<T>.InvalidMessageTypeReason, nameof(T));

        var scheduleMessagePipe = new ScheduleSendPipe<T>(pipe, dueAt);

        var endpoint = await _sendEndpointProvider.GetSendEndpointAsync(destinationAddress, cancellationToken: cancellationToken).ConfigureAwait(false);

        await endpoint.SendAsync(message, scheduleMessagePipe, cancellationToken).ConfigureAwait(false);

        return new ScheduledMessageHandle<T>(scheduleMessagePipe.ScheduledMessageId ?? NewId.NextGuid(), dueAt, destinationAddress, message);
    }

    public Task CancelScheduledSendAsync(Guid tokenId, CancellationToken cancellationToken)
    {
        if (cancellationToken.IsCancellationRequested) return global::System.Threading.Tasks.Task.FromCanceled(cancellationToken); return Task.CompletedTask;
    }

    public async Task CancelScheduledSendAsync(Uri destinationAddress, Guid tokenId, CancellationToken cancellationToken)
    {
        var endpoint = await _sendEndpointProvider.GetSendEndpointAsync(destinationAddress, cancellationToken: cancellationToken).ConfigureAwait(false);

        await endpoint.SendAsync<CancelScheduledMessage>(new
        {
            InVar.Timestamp,
            TokenId = tokenId
        }, cancellationToken).ConfigureAwait(false);
    }
}
