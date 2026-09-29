using System;
using System.Threading;
using System.Threading.Tasks;

namespace ViciOne.ServiceBus.Scheduling;

/// <summary>Builds scheduler commands and delegates their transport-specific dispatch.</summary>
public abstract class BaseScheduleMessageProvider :
    IScheduleMessageProvider,
    Advanced.IScheduleCancellationCapability
{
    /// <inheritdoc />
    public virtual Advanced.ScheduleCancellationMode CancellationMode => Advanced.ScheduleCancellationMode.Unknown;

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

        var tokenId = ScheduleTokenIdCache<T>.GetTokenId(message);
        var command = new ScheduleMessageCommand<T>(dueAt, destinationAddress, message, tokenId);
        var scheduleMessagePipe = new ScheduleMessageContextPipe<T>(message, pipe, command);

        await ScheduleSendAsync(command, scheduleMessagePipe, cancellationToken).ConfigureAwait(false);

        Guid acceptedTokenId = scheduleMessagePipe.AcceptResult();
        return new ScheduledMessageHandle<T>(acceptedTokenId, command.DueAt,
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
    readonly ScheduleMessageCommand<T> _command;
    readonly object _resultLock = new();
    Guid? _configuredTokenId;
    Guid? _acceptedTokenId;

    public ScheduleMessageContextPipe(T payload, IPipe<SendContext<T>> pipe, ScheduleMessageCommand<T> command)
    {
        _payload = payload ?? throw new ArgumentNullException(nameof(payload));
        _pipe = pipe ?? throw new ArgumentNullException(nameof(pipe));
        _command = command ?? throw new ArgumentNullException(nameof(command));
    }

    public async Task SendAsync(SendContext<ScheduleMessage> context)
    {
        lock (_resultLock)
        {
            if (_acceptedTokenId.HasValue)
                throw new InvalidOperationException("An accepted scheduling pipe cannot be applied to another send context.");
        }

        Guid originalTokenId = _command.TokenId;
        context.ScheduledMessageId = originalTokenId;
        bool appliedDefaultCorrelation = !context.CorrelationId.HasValue;
        context.CorrelationId ??= originalTokenId;

        if (_pipe.IsNotEmpty())
        {
            SendContext<T> proxy = context.CreateProxy(_payload);

            await _pipe.SendAsync(proxy).ConfigureAwait(false);
        }

        Guid finalTokenId = context.ScheduledMessageId ?? originalTokenId;
        if (finalTokenId != originalTokenId && context.BodyLength.HasValue)
            throw new InvalidOperationException("The scheduling token cannot change after the command body has been serialized.");

        context.ScheduledMessageId = finalTokenId;
        context.Headers.Set(MessageHeaders.SchedulingTokenId, finalTokenId.ToString("D"));
        if (appliedDefaultCorrelation && context.CorrelationId == originalTokenId)
            context.CorrelationId = finalTokenId;

        lock (_resultLock)
        {
            if (_acceptedTokenId.HasValue)
                throw new InvalidOperationException("The scheduling pipe completed after its send was accepted.");

            _command.TokenId = finalTokenId;
            _configuredTokenId = finalTokenId;
        }
    }

    internal Guid AcceptResult()
    {
        lock (_resultLock)
        {
            if (_acceptedTokenId is { } acceptedTokenId)
                return acceptedTokenId;

            if (_configuredTokenId is not { } configuredTokenId)
                throw new InvalidOperationException("The send completed without applying its scheduling pipe.");

            _acceptedTokenId = configuredTokenId;
            return configuredTokenId;
        }
    }

    void IProbeSite.Probe(ProbeContext context)
    {
        _pipe.Probe(context);
    }
}
