using System;
using System.Threading;
using System.Threading.Tasks;

namespace ViciOne.ServiceBus.Scheduling;

/// <summary>Builds scheduler commands and delegates their transport-specific dispatch.</summary>
public abstract class BaseScheduleMessageProvider :
    IScheduleMessageProvider
{
    /// <inheritdoc />
    public async Task<ScheduledMessage<T>> ScheduleSendAsync<T>(Uri destinationAddress, DateTimeOffset dueAt, T message,
        IPipe<SendContext<T>> pipe, CancellationToken cancellationToken)
        where T : class
    {
        ArgumentNullException.ThrowIfNull(destinationAddress);
        ArgumentNullException.ThrowIfNull(message);
        ArgumentNullException.ThrowIfNull(pipe);

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

    /// <inheritdoc />
    public Task CancelScheduledSendAsync(Guid tokenId, CancellationToken cancellationToken)
    {
        return CancelScheduledSendAsync(tokenId, null, cancellationToken);
    }

    /// <inheritdoc />
    public Task CancelScheduledSendAsync(Uri destinationAddress, Guid tokenId, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(destinationAddress);
        return CancelScheduledSendAsync(tokenId, destinationAddress, cancellationToken);
    }

    /// <summary>Dispatches a scheduling command through the provider-specific channel.</summary>
    /// <param name="message">The scheduling command.</param>
    /// <param name="pipe">Applies the original message's send configuration to the command.</param>
    /// <param name="cancellationToken">Cancels command dispatch.</param>
    /// <returns>A task that completes when the scheduler channel accepts the command.</returns>
    protected abstract Task ScheduleSendAsync(ScheduleMessage message, IPipe<SendContext<ScheduleMessage>> pipe, CancellationToken cancellationToken);

    /// <summary>Dispatches cancellation for a previously accepted scheduling token.</summary>
    /// <param name="tokenId">The scheduling token.</param>
    /// <param name="destinationAddress">The original destination when required by the provider.</param>
    /// <param name="cancellationToken">Cancels command dispatch.</param>
    /// <returns>A task that completes when the scheduler channel accepts the cancellation.</returns>
    protected abstract Task CancelScheduledSendAsync(Guid tokenId, Uri? destinationAddress, CancellationToken cancellationToken);
}


/// <summary>
/// Applies the original message's send pipe to a remote scheduler command while preserving its scheduling token.
/// </summary>
/// <typeparam name="T">The message type.</typeparam>
sealed class ScheduleMessageContextPipe<T> :
    IPipe<SendContext<ScheduleMessage>>
    where T : class
{
    readonly T _payload;
    readonly IPipe<SendContext<T>> _pipe;
    SendContext? _context;

    Guid? _scheduledMessageId;

    public ScheduleMessageContextPipe(T payload, IPipe<SendContext<T>> pipe)
    {
        _payload = payload ?? throw new ArgumentNullException(nameof(payload));
        _pipe = pipe ?? throw new ArgumentNullException(nameof(pipe));
    }

    public Guid? ScheduledMessageId
    {
        get => _context?.ScheduledMessageId ?? _scheduledMessageId;
        set => _scheduledMessageId = value;
    }

    public async Task SendAsync(SendContext<ScheduleMessage> context)
    {
        _context = context;

        context.ScheduledMessageId = _scheduledMessageId;
        context.CorrelationId ??= _scheduledMessageId;

        if (_pipe.IsNotEmpty())
        {
            SendContext<T> proxy = context.CreateProxy(_payload);

            await _pipe.SendAsync(proxy).ConfigureAwait(false);
        }
    }

    void IProbeSite.Probe(ProbeContext context)
    {
        _pipe.Probe(context);
    }
}
