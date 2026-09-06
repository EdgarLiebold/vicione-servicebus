using System;

namespace ViciOne.ServiceBus.Testing;

/// <summary>Carries published message data.</summary>
/// <typeparam name="T">The value type.</typeparam>
public class PublishedMessage<T> :
    IPublishedMessage<T>
    where T : class
{
    readonly PublishContext<T> _context;

    /// <summary>Initializes a new instance.</summary>
    /// <param name="context">The context associated with the operation.</param>
    /// <param name="exception">The exception associated with the operation.</param>
    public PublishedMessage(PublishContext<T> context, Exception? exception = null)
        : this(context, exception, TimeProvider.System)
    {
    }

    /// <summary>Initializes a new instance.</summary>
    /// <param name="context">The context associated with the operation.</param>
    /// <param name="exception">The exception associated with the operation.</param>
    /// <param name="timeProvider">The time source used by the operation.</param>
    public PublishedMessage(PublishContext<T> context, Exception? exception, TimeProvider timeProvider)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(timeProvider);

        _context = context;
        Exception = exception;

        ElementId = _context.MessageId;

        DateTimeOffset now = timeProvider.GetUtcNow();
        StartTime = context.SentTime ?? now;
        ElapsedTime = now - StartTime;
    }

    /// <summary>Gets the element id.</summary>
    public Guid? ElementId { get; }
    SendContext IPublishedMessage.Context => _context;
    /// <summary>Gets the start time.</summary>
    public DateTimeOffset StartTime { get; }
    /// <summary>Gets the elapsed time.</summary>
    public TimeSpan ElapsedTime { get; }
    /// <summary>Gets the exception.</summary>
    public Exception? Exception { get; }
    /// <summary>Gets the message type.</summary>
    public Type MessageType => typeof(T);
    /// <summary>Gets the short type name.</summary>
    public string ShortTypeName => TypeCache<T>.ShortName;
    object IPublishedMessage.MessageObject => _context.Message;
    PublishContext<T> IPublishedMessage<T>.Context => _context;
}
