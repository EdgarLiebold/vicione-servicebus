using System;
using System.Threading;
using System.Threading.Tasks;
using ViciOne.ServiceBus.Transports;

namespace ViciOne.ServiceBus.Scheduling;

/// <summary>Applies scheduling identity and transport delay to a send context.</summary>
/// <typeparam name="TMessage">The message type.</typeparam>
public sealed class ScheduleSendPipe<TMessage> :
    SendContextPipeAdapter<TMessage>
    where TMessage : class
{
    readonly DateTimeOffset _dueAt;
    readonly TimeProvider? _timeProvider;
    readonly object _resultLock = new();
    SendContext? _configuredContext;
    ScheduleSendResult? _acceptedResult;

    Guid? _scheduledMessageId;

    /// <summary>Creates a scheduling pipe that obtains its clock from each send context.</summary>
    /// <param name="pipe">The message-specific send pipeline.</param>
    /// <param name="dueAt">The requested delivery time.</param>
    public ScheduleSendPipe(IPipe<SendContext<TMessage>> pipe, DateTimeOffset dueAt)
        : base(new SchedulingTokenPipe(pipe ?? throw new ArgumentNullException(nameof(pipe))))
    {
        _dueAt = dueAt;
    }

    /// <summary>Creates a scheduling pipe that calculates delays from a specified clock.</summary>
    /// <param name="pipe">The message-specific send pipeline.</param>
    /// <param name="dueAt">The requested delivery time.</param>
    /// <param name="timeProvider">The clock used to calculate the transport delay.</param>
    public ScheduleSendPipe(IPipe<SendContext<TMessage>> pipe, DateTimeOffset dueAt, TimeProvider timeProvider)
        : base(new SchedulingTokenPipe(pipe ?? throw new ArgumentNullException(nameof(pipe))))
    {
        _dueAt = dueAt;
        _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
    }

    /// <summary>Gets or sets the scheduling token applied to the send context.</summary>
    public Guid? ScheduledMessageId
    {
        get
        {
            lock (_resultLock)
            {
                if (_acceptedResult is { } acceptedResult)
                    return acceptedResult.ScheduledMessageId;

                return _configuredContext?.ScheduledMessageId ?? _scheduledMessageId;
            }
        }
        set
        {
            lock (_resultLock)
                _scheduledMessageId = value;
        }
    }

    /// <summary>Gets the message identifier assigned by the send pipeline.</summary>
    public Guid? MessageId
    {
        get
        {
            lock (_resultLock)
            {
                if (_acceptedResult is { } acceptedResult)
                    return acceptedResult.MessageId;

                return _configuredContext?.MessageId;
            }
        }
    }

    internal ScheduleSendResult AcceptResult()
    {
        lock (_resultLock)
        {
            if (_acceptedResult is { } acceptedResult)
                return acceptedResult;

            if (_configuredContext is not { } context)
                throw new InvalidOperationException("The send completed without applying its scheduling pipe.");

            var result = new ScheduleSendResult(context.ScheduledMessageId, context.MessageId);
            _acceptedResult = result;
            _configuredContext = null;
            return result;
        }
    }

    /// <summary>Applies scheduling metadata to the message context.</summary>
    /// <param name="context">The message send context.</param>
    protected override void Send(SendContext<TMessage> context)
    {
        Apply(context);
    }

    /// <summary>Applies scheduling metadata when the pipe is invoked through its untyped contract.</summary>
    /// <typeparam name="T">The runtime message contract.</typeparam>
    /// <param name="context">The runtime message send context.</param>
    protected override void Send<T>(SendContext<T> context)
    {
        Apply(context);
    }

    void Apply(SendContext context)
    {
        lock (_resultLock)
        {
            if (_acceptedResult.HasValue)
                throw new InvalidOperationException("An accepted scheduling pipe cannot be applied to another send context.");

            _configuredContext = context;
            context.ScheduledMessageId = _scheduledMessageId;
        }

        TimeProvider timeProvider = _timeProvider ?? context.GetTimeProvider();
        TimeSpan delay = _dueAt - timeProvider.GetUtcNow();

        if (delay > TimeSpan.Zero)
            context.Delay = delay;

        if (context.ScheduledMessageId.HasValue)
            context.Headers.Set(MessageHeaders.SchedulingTokenId, context.ScheduledMessageId.Value.ToString("D"));
    }

    internal readonly record struct ScheduleSendResult(Guid? ScheduledMessageId, Guid? MessageId);

    sealed class SchedulingTokenPipe : IPipe<SendContext<TMessage>>, ISendContextPipe
    {
        readonly IPipe<SendContext<TMessage>> _pipe;

        public SchedulingTokenPipe(IPipe<SendContext<TMessage>> pipe)
        {
            _pipe = pipe;
        }

        public async Task SendAsync(SendContext<TMessage> context)
        {
            Guid? originalTokenId = context.ScheduledMessageId;

            if (_pipe.IsNotEmpty())
                await (_pipe.SendAsync(context) ?? throw new InvalidOperationException("The wrapped typed send pipe returned no task."))
                    .ConfigureAwait(false);

            ReconcileToken(context, originalTokenId);
        }

        async Task ISendContextPipe.SendAsync<T>(SendContext<T> context, CancellationToken cancellationToken)
        {
            Guid? originalTokenId = context.ScheduledMessageId;

            if (_pipe is ISendContextPipe sendContextPipe)
                await (sendContextPipe.SendAsync(context, cancellationToken)
                    ?? throw new InvalidOperationException("The wrapped send-context pipe returned no task.")).ConfigureAwait(false);

            ReconcileToken(context, originalTokenId);
        }

        public void Probe(ProbeContext context)
        {
            _pipe.Probe(context);
        }

        static void ReconcileToken(SendContext context, Guid? originalTokenId)
        {
            context.ScheduledMessageId ??= originalTokenId;

            if (context.ScheduledMessageId is Guid tokenId)
                context.Headers.Set(MessageHeaders.SchedulingTokenId, tokenId.ToString("D"));
        }
    }
}
