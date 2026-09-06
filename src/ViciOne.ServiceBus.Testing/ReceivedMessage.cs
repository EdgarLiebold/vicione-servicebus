using System;

namespace ViciOne.ServiceBus.Testing;

/// <summary>Carries received message data.</summary>
/// <typeparam name="T">The value type.</typeparam>
public class ReceivedMessage<T> :
    IReceivedMessage<T>
    where T : class
{
    readonly ConsumeContext<T> _context;
    readonly Exception? _exception;

    /// <summary>Initializes a new instance.</summary>
    /// <param name="context">The context associated with the operation.</param>
    /// <param name="exception">The exception associated with the operation.</param>
    public ReceivedMessage(ConsumeContext<T> context, Exception? exception = null)
        : this(context, exception, TimeProvider.System)
    {
    }

    /// <summary>Initializes a new instance.</summary>
    /// <param name="context">The context associated with the operation.</param>
    /// <param name="exception">The exception associated with the operation.</param>
    /// <param name="timeProvider">The time source used by the operation.</param>
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

    /// <summary>Gets the element id.</summary>
    public Guid? ElementId { get; }
    ConsumeContext IReceivedMessage.Context => _context.Advanced();
    /// <summary>Gets the start time.</summary>
    public DateTimeOffset StartTime { get; }
    /// <summary>Gets the elapsed time.</summary>
    public TimeSpan ElapsedTime { get; }
    Exception? IReceivedMessage.Exception => _exception;
    Type IReceivedMessage.MessageType => typeof(T);
    string IReceivedMessage.ShortTypeName => TypeCache<T>.ShortName;
    object IReceivedMessage.MessageObject => _context.Message;
    ConsumeContext<T> IReceivedMessage<T>.Context => _context;
}
