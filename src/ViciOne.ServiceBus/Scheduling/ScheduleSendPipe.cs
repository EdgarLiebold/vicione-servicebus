using System;
using ViciOne.ServiceBus.Transports;

namespace ViciOne.ServiceBus.Scheduling;

/// <summary>
/// For transport-based schedulers, used to invoke the <see cref="SendContext{T}" /> pipe and
/// manage the ScheduledMessageId, as well as set the transport delay property
/// </summary>
/// <typeparam name="TMessage">The message type</typeparam>
public class ScheduleSendPipe<TMessage> :
    SendContextPipeAdapter<TMessage>
    where TMessage : class
{
    readonly DateTimeOffset _dueAt;
    readonly TimeProvider _timeProvider = null!;
    SendContext _context = null!;

    Guid? _scheduledMessageId;

    public ScheduleSendPipe(IPipe<SendContext<TMessage>> pipe, DateTimeOffset dueAt)
        : base(pipe)
    {
        _dueAt = dueAt;
    }

    public ScheduleSendPipe(IPipe<SendContext<TMessage>> pipe, DateTimeOffset dueAt, TimeProvider timeProvider)
        : base(pipe)
    {
        _dueAt = dueAt;
        _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
    }

    public Guid? ScheduledMessageId
    {
        get => _context?.ScheduledMessageId ?? _scheduledMessageId;
        set => _scheduledMessageId = value;
    }

    public Guid? MessageId => _context?.MessageId;

    protected override void Send(SendContext<TMessage> context)
    {
        _context = context;
        _context.ScheduledMessageId = _scheduledMessageId;

        TimeProvider timeProvider = _timeProvider ?? context.GetTimeProvider();
        TimeSpan delay = _dueAt - timeProvider.GetUtcNow();

        if (delay > TimeSpan.Zero)
            context.Delay = delay;

        if (ScheduledMessageId.HasValue)
            context.Headers.Set(MessageHeaders.SchedulingTokenId, ScheduledMessageId.Value.ToString("D"));
    }

    protected override void Send<T>(SendContext<T> context)
    {
    }
}
