using System;
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
    SendContext _context = null!;

    Guid? _scheduledMessageId;

    /// <summary>Creates a scheduling pipe that obtains its clock from each send context.</summary>
    /// <param name="pipe">The message-specific send pipeline.</param>
    /// <param name="dueAt">The requested delivery time.</param>
    public ScheduleSendPipe(IPipe<SendContext<TMessage>> pipe, DateTimeOffset dueAt)
        : base(pipe ?? throw new ArgumentNullException(nameof(pipe)))
    {
        _dueAt = dueAt;
    }

    /// <summary>Creates a scheduling pipe that calculates delays from a specified clock.</summary>
    /// <param name="pipe">The message-specific send pipeline.</param>
    /// <param name="dueAt">The requested delivery time.</param>
    /// <param name="timeProvider">The clock used to calculate the transport delay.</param>
    public ScheduleSendPipe(IPipe<SendContext<TMessage>> pipe, DateTimeOffset dueAt, TimeProvider timeProvider)
        : base(pipe ?? throw new ArgumentNullException(nameof(pipe)))
    {
        _dueAt = dueAt;
        _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
    }

    /// <summary>Gets or sets the scheduling token applied to the send context.</summary>
    public Guid? ScheduledMessageId
    {
        get => _context?.ScheduledMessageId ?? _scheduledMessageId;
        set => _scheduledMessageId = value;
    }

    /// <summary>Gets the message identifier assigned by the send pipeline.</summary>
    public Guid? MessageId => _context?.MessageId;

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
        _context = context;
        context.ScheduledMessageId = _scheduledMessageId;

        TimeProvider timeProvider = _timeProvider ?? context.GetTimeProvider();
        TimeSpan delay = _dueAt - timeProvider.GetUtcNow();

        if (delay > TimeSpan.Zero)
            context.Delay = delay;

        if (context.ScheduledMessageId.HasValue)
            context.Headers.Set(MessageHeaders.SchedulingTokenId, context.ScheduledMessageId.Value.ToString("D"));
    }
}
