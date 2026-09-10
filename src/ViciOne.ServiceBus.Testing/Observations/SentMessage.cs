using System;

namespace ViciOne.ServiceBus.Testing;

/// <summary>Captures the context, timing, and optional failure of one message-send attempt.</summary>
/// <typeparam name="TMessage">The sent message contract.</typeparam>
public sealed class SentMessage<TMessage> :
    ISentMessage<TMessage>
    where TMessage : class
{
    readonly SendContext<TMessage> _context;
    readonly Exception? _exception;

    /// <summary>Captures a send using the system clock.</summary>
    /// <param name="context">The send context to record.</param>
    /// <param name="exception">The send-pipeline exception, or <see langword="null"/> for success.</param>
    public SentMessage(SendContext<TMessage> context, Exception? exception = null)
        : this(context, exception, TimeProvider.System)
    {
    }

    /// <summary>Captures a send.</summary>
    /// <param name="context">The send context to record.</param>
    /// <param name="exception">The send-pipeline exception, or <see langword="null"/> for success.</param>
    /// <param name="timeProvider">The clock used to measure elapsed time.</param>
    public SentMessage(SendContext<TMessage> context, Exception? exception, TimeProvider timeProvider)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(timeProvider);

        _context = context;
        _exception = exception;

        ElementId = _context.MessageId;

        DateTimeOffset now = timeProvider.GetUtcNow();
        StartTime = context.SentTime ?? now;
        ElapsedTime = now - StartTime;
    }

    /// <inheritdoc />
    public Guid? ElementId { get; }
    SendContext ISentMessage.Context => _context;
    /// <inheritdoc />
    public DateTimeOffset StartTime { get; }
    /// <inheritdoc />
    public TimeSpan ElapsedTime { get; }
    object ISentMessage.MessageObject => _context.Message;
    Exception? ISentMessage.Exception => _exception;
    Type ISentMessage.MessageType => typeof(TMessage);
    /// <inheritdoc />
    public string ShortTypeName => TypeCache<TMessage>.ShortName;
    SendContext<TMessage> ISentMessage<TMessage>.Context => _context;
}
