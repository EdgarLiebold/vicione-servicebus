using System;
using System.Threading;
using System.Threading.Tasks;

namespace ViciOne.ServiceBus.Scheduling;

/// <summary>
/// Provides a base schedule message provider implementation.
/// </summary>
public abstract class BaseScheduleMessageProvider :
    IScheduleMessageProvider
{
    /// <summary>
    /// Schedules send.
    /// </summary>
    /// <typeparam name="T">The t type.</typeparam>
    /// <param name="destinationAddress">The destination address value.</param>
    /// <param name="dueAt">The due at value.</param>
    /// <param name="message">The message value.</param>
    /// <param name="pipe">The pipe value.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The result of the operation.</returns>
    public async Task<ScheduledMessage<T>> ScheduleSendAsync<T>(Uri destinationAddress, DateTimeOffset dueAt, T message,
        IPipe<SendContext<T>> pipe, CancellationToken cancellationToken)
        where T : class
    {
        if (!MessageTypeCache<T>.IsValidMessageType)
            throw new ArgumentException(MessageTypeCache<T>.InvalidMessageTypeReason, nameof(T));

        var scheduleMessagePipe = new ScheduleMessageContextPipe<T>(message, pipe);

        var tokenId = ScheduleTokenIdCache<T>.GetTokenId(message);

        scheduleMessagePipe.ScheduledMessageId = tokenId;

        ScheduleMessage command = new ScheduleMessageCommand<T>(dueAt, destinationAddress, message, tokenId);

        await ScheduleSendAsync(command, scheduleMessagePipe, cancellationToken).ConfigureAwait(false);

        return new ScheduledMessageHandle<T>(scheduleMessagePipe.ScheduledMessageId ?? command.TokenId, command.DueAt,
            command.Destination, message);
    }

    /// <summary>
    /// Determines whether the current value can cel scheduled send.
    /// </summary>
    /// <param name="tokenId">The token id value.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The result of the operation.</returns>
    public Task CancelScheduledSendAsync(Guid tokenId, CancellationToken cancellationToken)
    {
        return CancelScheduledSendAsync(tokenId, null, cancellationToken);
    }

    /// <summary>
    /// Determines whether the current value can cel scheduled send.
    /// </summary>
    /// <param name="destinationAddress">The destination address value.</param>
    /// <param name="tokenId">The token id value.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The result of the operation.</returns>
    public Task CancelScheduledSendAsync(Uri destinationAddress, Guid tokenId, CancellationToken cancellationToken)
    {
        return CancelScheduledSendAsync(tokenId, destinationAddress, cancellationToken);
    }

    /// <summary>
    /// Schedules send.
    /// </summary>
    /// <param name="message">The message value.</param>
    /// <param name="pipe">The pipe value.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The result of the operation.</returns>
    protected abstract Task ScheduleSendAsync(ScheduleMessage message, IPipe<SendContext<ScheduleMessage>> pipe, CancellationToken cancellationToken);

    /// <summary>
    /// Determines whether the current value can cel scheduled send.
    /// </summary>
    /// <param name="tokenId">The token id value.</param>
    /// <param name="destinationAddress">The destination address value.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The result of the operation.</returns>
    protected abstract Task CancelScheduledSendAsync(Guid tokenId, Uri? destinationAddress, CancellationToken cancellationToken);
}


/// <summary>
/// For remote endpoint schedulers, used to invoke the <see cref="SendContext{T}" /> pipe and
/// manage the ScheduledMessageId
/// </summary>
/// <typeparam name="T">The message type</typeparam>
class ScheduleMessageContextPipe<T> :
    IPipe<SendContext<ScheduleMessage>>
    where T : class
{
    readonly T _payload;
    readonly IPipe<SendContext<T>> _pipe;
    SendContext _context = null!;

    Guid? _scheduledMessageId;

    public ScheduleMessageContextPipe(T payload, IPipe<SendContext<T>> pipe)
    {
        _payload = payload;
        _pipe = pipe;
    }

    public Guid? ScheduledMessageId
    {
        get => _context?.ScheduledMessageId ?? _scheduledMessageId;
        set => _scheduledMessageId = value;
    }

    public async Task SendAsync(SendContext<ScheduleMessage> context)
    {
        _context = context;

        _context.ScheduledMessageId = _scheduledMessageId;
        _context.CorrelationId ??= _scheduledMessageId;

        if (_pipe.IsNotEmpty())
        {
            SendContext<T> proxy = context.CreateProxy(_payload);

            await _pipe.SendAsync(proxy).ConfigureAwait(false);
        }
    }

    void IProbeSite.Probe(ProbeContext context)
    {
        _pipe?.Probe(context);
    }
}
