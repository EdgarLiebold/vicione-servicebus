using System;
using System.Threading;
using System.Threading.Tasks;

namespace ViciOne.ServiceBus.Scheduling;

public class DelayedScheduleMessageProvider :
    IScheduleMessageProvider
{
    readonly ISendEndpointProvider _sendEndpointProvider;
    readonly TimeProvider _timeProvider = null!;

    public DelayedScheduleMessageProvider(ISendEndpointProvider sendEndpointProvider)
    {
        _sendEndpointProvider = sendEndpointProvider;
    }

    internal DelayedScheduleMessageProvider(ISendEndpointProvider sendEndpointProvider, TimeProvider timeProvider)
    {
        _sendEndpointProvider = sendEndpointProvider;
        _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
    }

    public async Task<ScheduledMessage<T>> ScheduleSendAsync<T>(Uri destinationAddress, DateTimeOffset dueAt, T message, IPipe<SendContext<T>> pipe,
        CancellationToken cancellationToken)
        where T : class
    {
        if (!MessageTypeCache<T>.IsValidMessageType)
            throw new ArgumentException(MessageTypeCache<T>.InvalidMessageTypeReason, nameof(T));

        var scheduleMessagePipe = _timeProvider == null
            ? new ScheduleSendPipe<T>(pipe, dueAt)
            : new ScheduleSendPipe<T>(pipe, dueAt, _timeProvider);

        var tokenId = ScheduleTokenIdCache<T>.GetTokenId(message);

        scheduleMessagePipe.ScheduledMessageId = tokenId;

        var schedulerEndpoint = await _sendEndpointProvider.GetSendEndpointAsync(destinationAddress, cancellationToken: cancellationToken).ConfigureAwait(false);

        await schedulerEndpoint.SendAsync(message, scheduleMessagePipe, cancellationToken).ConfigureAwait(false);

        return new ScheduledMessageHandle<T>(scheduleMessagePipe.ScheduledMessageId ?? NewId.NextGuid(), dueAt, destinationAddress, message);
    }

    public Task CancelScheduledSendAsync(Guid tokenId, CancellationToken cancellationToken)
    {
        if (cancellationToken.IsCancellationRequested) return global::System.Threading.Tasks.Task.FromCanceled(cancellationToken); return Task.CompletedTask;
    }

    public Task CancelScheduledSendAsync(Uri destinationAddress, Guid tokenId, CancellationToken cancellationToken)
    {
        if (cancellationToken.IsCancellationRequested) return global::System.Threading.Tasks.Task.FromCanceled(cancellationToken); return Task.CompletedTask;
    }
}
