using System;

namespace ViciOne.ServiceBus.Testing;

/// <summary>
/// Provides a received message implementation.
/// </summary>
/// <typeparam name="T">The t type.</typeparam>
public class ReceivedMessage<T> :
    IReceivedMessage<T>
    where T : class
{
    readonly ConsumeContext<T> _context;
    readonly Exception? _exception;

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="context">The operation context.</param>
    /// <param name="exception">The exception associated with the operation.</param>
    public ReceivedMessage(ConsumeContext<T> context, Exception? exception = null)
        : this(context, exception, TimeProvider.System)
    {
    }

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="context">The operation context.</param>
    /// <param name="exception">The exception associated with the operation.</param>
    /// <param name="timeProvider">The time provider value.</param>
    public ReceivedMessage(ConsumeContext<T> context, Exception? exception, TimeProvider timeProvider)
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

    /// <summary>
    /// Gets the element id value.
    /// </summary>
    public Guid? ElementId { get; }
    ConsumeContext IReceivedMessage.Context => _context.Advanced();
    /// <summary>
    /// Gets the start time value.
    /// </summary>
    public DateTimeOffset StartTime { get; }
    /// <summary>
    /// Gets the elapsed time value.
    /// </summary>
    public TimeSpan ElapsedTime { get; }
    Exception? IReceivedMessage.Exception => _exception;
    Type IReceivedMessage.MessageType => typeof(T);
    string IReceivedMessage.ShortTypeName => TypeCache<T>.ShortName;
    object IReceivedMessage.MessageObject => _context.Message;
    ConsumeContext<T> IReceivedMessage<T>.Context => _context;
}
