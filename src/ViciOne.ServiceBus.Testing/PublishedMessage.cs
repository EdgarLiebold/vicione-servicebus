using System;

namespace ViciOne.ServiceBus.Testing;

/// <summary>
/// Provides a published message implementation.
/// </summary>
/// <typeparam name="T">The t type.</typeparam>
public class PublishedMessage<T> :
    IPublishedMessage<T>
    where T : class
{
    readonly PublishContext<T> _context;

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="context">The operation context.</param>
    /// <param name="exception">The exception associated with the operation.</param>
    public PublishedMessage(PublishContext<T> context, Exception? exception = null)
        : this(context, exception, TimeProvider.System)
    {
    }

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="context">The operation context.</param>
    /// <param name="exception">The exception associated with the operation.</param>
    /// <param name="timeProvider">The time provider value.</param>
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

    /// <summary>
    /// Gets the element id value.
    /// </summary>
    public Guid? ElementId { get; }
    SendContext IPublishedMessage.Context => _context;
    /// <summary>
    /// Gets the start time value.
    /// </summary>
    public DateTimeOffset StartTime { get; }
    /// <summary>
    /// Gets the elapsed time value.
    /// </summary>
    public TimeSpan ElapsedTime { get; }
    /// <summary>
    /// Gets the exception value.
    /// </summary>
    public Exception? Exception { get; }
    /// <summary>
    /// Gets the message type value.
    /// </summary>
    public Type MessageType => typeof(T);
    /// <summary>
    /// Gets the short type name value.
    /// </summary>
    public string ShortTypeName => TypeCache<T>.ShortName;
    object IPublishedMessage.MessageObject => _context.Message;
    PublishContext<T> IPublishedMessage<T>.Context => _context;
}
