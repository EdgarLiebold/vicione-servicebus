using System;

namespace ViciOne.ServiceBus.Testing;

/// <summary>
/// Provides a sent message implementation.
/// </summary>
/// <typeparam name="T">The t type.</typeparam>
public class SentMessage<T> :
    ISentMessage<T>
    where T : class
{
    readonly SendContext<T> _context;
    readonly Exception? _exception;

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="context">The operation context.</param>
    /// <param name="exception">The exception associated with the operation.</param>
    public SentMessage(SendContext<T> context, Exception? exception = null)
        : this(context, exception, TimeProvider.System)
    {
    }

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="context">The operation context.</param>
    /// <param name="exception">The exception associated with the operation.</param>
    /// <param name="timeProvider">The time provider value.</param>
    public SentMessage(SendContext<T> context, Exception? exception, TimeProvider timeProvider)
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

    /// <summary>
    /// Gets the element id value.
    /// </summary>
    public Guid? ElementId { get; }
    SendContext ISentMessage.Context => _context;
    /// <summary>
    /// Gets the start time value.
    /// </summary>
    public DateTimeOffset StartTime { get; }
    /// <summary>
    /// Gets the elapsed time value.
    /// </summary>
    public TimeSpan ElapsedTime { get; }
    object ISentMessage.MessageObject => _context.Message;
    Exception? ISentMessage.Exception => _exception;
    Type ISentMessage.MessageType => typeof(T);
    /// <summary>
    /// Gets the short type name value.
    /// </summary>
    public string ShortTypeName => TypeCache<T>.ShortName;
    SendContext<T> ISentMessage<T>.Context => _context;
}
