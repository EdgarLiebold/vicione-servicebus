using System;
using System.Threading;
using System.Threading.Tasks;

namespace ViciOne.ServiceBus.Scheduling;

/// <summary>Provides delayed schedule message services.</summary>
public class DelayedScheduleMessageProvider :
    IScheduleMessageProvider
{
    readonly ISendEndpointProvider _sendEndpointProvider;
    readonly TimeProvider _timeProvider = null!;

    /// <summary>Initializes a new instance.</summary>
    /// <param name="sendEndpointProvider">The send endpoint provider.</param>
    public DelayedScheduleMessageProvider(ISendEndpointProvider sendEndpointProvider)
    {
        _sendEndpointProvider = sendEndpointProvider;
    }

    internal DelayedScheduleMessageProvider(ISendEndpointProvider sendEndpointProvider, TimeProvider timeProvider)
    {
        _sendEndpointProvider = sendEndpointProvider;
        _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
    }

    /// <summary>Schedules send.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="destinationAddress">The destination address.</param>
    /// <param name="dueAt">The due at.</param>
    /// <param name="message">The message to process.</param>
    /// <param name="pipe">The pipeline stages to apply.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that produces the schedule send outcome.</returns>
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

    /// <summary>Determines whether the current value can cel scheduled send.</summary>
    /// <param name="tokenId">The token id.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public Task CancelScheduledSendAsync(Guid tokenId, CancellationToken cancellationToken)
    {
        if (cancellationToken.IsCancellationRequested) return global::System.Threading.Tasks.Task.FromCanceled(cancellationToken); return Task.CompletedTask;
    }

    /// <summary>Determines whether the current value can cel scheduled send.</summary>
    /// <param name="destinationAddress">The destination address.</param>
    /// <param name="tokenId">The token id.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public Task CancelScheduledSendAsync(Uri destinationAddress, Guid tokenId, CancellationToken cancellationToken)
    {
        if (cancellationToken.IsCancellationRequested) return global::System.Threading.Tasks.Task.FromCanceled(cancellationToken); return Task.CompletedTask;
    }
}
