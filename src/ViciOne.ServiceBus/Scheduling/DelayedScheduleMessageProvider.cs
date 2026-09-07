using System;
using System.Threading;
using System.Threading.Tasks;

namespace ViciOne.ServiceBus.Scheduling;

/// <summary>Schedules messages by applying a transport delivery delay.</summary>
public sealed class DelayedScheduleMessageProvider :
    IScheduleMessageProvider
{
    readonly ISendEndpointProvider _sendEndpointProvider;
    readonly TimeProvider _timeProvider;

    /// <summary>Creates a provider that schedules through the resolved destination endpoint.</summary>
    /// <param name="sendEndpointProvider">Resolves destination endpoints.</param>
    /// <param name="timeProvider">The clock used to calculate transport delays.</param>
    public DelayedScheduleMessageProvider(ISendEndpointProvider sendEndpointProvider, TimeProvider? timeProvider = null)
    {
        _sendEndpointProvider = sendEndpointProvider ?? throw new ArgumentNullException(nameof(sendEndpointProvider));
        _timeProvider = timeProvider ?? TimeProvider.System;
    }

    /// <inheritdoc />
    public async Task<ScheduledMessage<T>> ScheduleSendAsync<T>(Uri destinationAddress, DateTimeOffset dueAt, T message, IPipe<SendContext<T>> pipe,
        CancellationToken cancellationToken)
        where T : class
    {
        ArgumentNullException.ThrowIfNull(destinationAddress);
        ArgumentNullException.ThrowIfNull(message);
        ArgumentNullException.ThrowIfNull(pipe);

        if (!MessageTypeCache<T>.IsValidMessageType)
            throw new ArgumentException(MessageTypeCache<T>.InvalidMessageTypeReason, nameof(T));

        var scheduleMessagePipe = new ScheduleSendPipe<T>(pipe, dueAt, _timeProvider);

        var tokenId = ScheduleTokenIdCache<T>.GetTokenId(message);

        scheduleMessagePipe.ScheduledMessageId = tokenId;

        var schedulerEndpoint = await _sendEndpointProvider.GetSendEndpointAsync(destinationAddress, cancellationToken: cancellationToken).ConfigureAwait(false);

        await schedulerEndpoint.SendAsync(message, scheduleMessagePipe, cancellationToken).ConfigureAwait(false);

        return new ScheduledMessageHandle<T>(scheduleMessagePipe.ScheduledMessageId ?? NewId.NextGuid(), dueAt, destinationAddress, message);
    }

    /// <summary>Reports that transport-delayed messages cannot be recalled after acceptance.</summary>
    /// <param name="tokenId">The scheduling token assigned to the message.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public Task CancelScheduledSendAsync(Guid tokenId, CancellationToken cancellationToken)
    {
        return CancellationNotSupportedAsync(cancellationToken);
    }

    /// <summary>Reports that transport-delayed messages cannot be recalled after acceptance.</summary>
    /// <param name="destinationAddress">The destination that accepted the delayed message.</param>
    /// <param name="tokenId">The scheduling token assigned to the message.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public Task CancelScheduledSendAsync(Uri destinationAddress, Guid tokenId, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(destinationAddress);
        return CancellationNotSupportedAsync(cancellationToken);
    }

    static Task CancellationNotSupportedAsync(CancellationToken cancellationToken)
    {
        return cancellationToken.IsCancellationRequested
            ? Task.FromCanceled(cancellationToken)
            : Task.FromException(new NotSupportedException(
                "Transport-delayed messages cannot be canceled after the transport has accepted them."));
    }
}
