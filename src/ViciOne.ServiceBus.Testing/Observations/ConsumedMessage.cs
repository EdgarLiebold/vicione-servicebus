using System;

namespace ViciOne.ServiceBus.Testing;

/// <summary>Captures the context, timing, and optional failure of one message-consumption attempt.</summary>
/// <typeparam name="TMessage">The consumed message contract.</typeparam>
public sealed class ConsumedMessage<TMessage> :
    IConsumedMessage<TMessage>
    where TMessage : class
{
    readonly ConsumeContext<TMessage> _context;
    readonly Exception? _exception;

    /// <summary>Captures a consumption using the system clock.</summary>
    /// <param name="context">The consume context to record.</param>
    /// <param name="exception">The consume-pipeline exception, or <see langword="null"/> for success.</param>
    public ConsumedMessage(ConsumeContext<TMessage> context, Exception? exception = null)
        : this(context, exception, TimeProvider.System)
    {
    }

    /// <summary>Captures a consumption.</summary>
    /// <param name="context">The consume context to record.</param>
    /// <param name="exception">The consume-pipeline exception, or <see langword="null"/> for success.</param>
    /// <param name="timeProvider">The clock used to estimate the processing start time.</param>
    public ConsumedMessage(ConsumeContext<TMessage> context, Exception? exception, TimeProvider timeProvider)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(timeProvider);

        _context = context;
        _exception = exception;

        ElementId = _context.MessageId;

        ElapsedTime = context.Advanced().ReceiveContext.ElapsedTime;
        StartTime = timeProvider.GetUtcNow() - ElapsedTime;
        if (StartTime < context.SentTime)
            StartTime = context.SentTime.Value;
    }

    /// <inheritdoc />
    public Guid? ElementId { get; }
    ConsumeContext IConsumedMessage.Context => _context.Advanced();
    /// <inheritdoc />
    public DateTimeOffset StartTime { get; }
    /// <inheritdoc />
    public TimeSpan ElapsedTime { get; }
    Exception? IConsumedMessage.Exception => _exception;
    Type IConsumedMessage.MessageType => typeof(TMessage);
    string IConsumedMessage.ShortTypeName => TypeCache<TMessage>.ShortName;
    object IConsumedMessage.MessageObject => _context.Message;
    ConsumeContext<TMessage> IConsumedMessage<TMessage>.Context => _context;
}
