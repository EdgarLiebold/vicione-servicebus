using System;

namespace ViciOne.ServiceBus.Testing;

/// <summary>Captures the context, timing, and optional failure of one message-publication attempt.</summary>
/// <typeparam name="TMessage">The published message contract.</typeparam>
public sealed class PublishedMessage<TMessage> :
    IPublishedMessage<TMessage>
    where TMessage : class
{
    readonly PublishContext<TMessage> _context;

    /// <summary>Captures a publication using the system clock.</summary>
    /// <param name="context">The publish context to record.</param>
    /// <param name="exception">The publish-pipeline exception, or <see langword="null"/> for success.</param>
    public PublishedMessage(PublishContext<TMessage> context, Exception? exception = null)
        : this(context, exception, TimeProvider.System)
    {
    }

    /// <summary>Captures a publication.</summary>
    /// <param name="context">The publish context to record.</param>
    /// <param name="exception">The publish-pipeline exception, or <see langword="null"/> for success.</param>
    /// <param name="timeProvider">The clock used to measure elapsed time.</param>
    public PublishedMessage(PublishContext<TMessage> context, Exception? exception, TimeProvider timeProvider)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(timeProvider);

        _context = context;
        Exception = exception;

        ElementId = _context.MessageId;

        DateTimeOffset now = timeProvider.GetUtcNow();
        StartTime = context.SentTime is { } sentTime && sentTime <= now ? sentTime : now;
        ElapsedTime = now - StartTime;
    }

    /// <inheritdoc />
    public Guid? ElementId { get; }
    SendContext IPublishedMessage.Context => _context;
    /// <inheritdoc />
    public DateTimeOffset StartTime { get; }
    /// <inheritdoc />
    public TimeSpan ElapsedTime { get; }
    /// <inheritdoc />
    public Exception? Exception { get; }
    /// <inheritdoc />
    public Type MessageType => typeof(TMessage);
    /// <inheritdoc />
    public string ShortTypeName => TypeCache<TMessage>.ShortName;
    object IPublishedMessage.MessageObject => _context.Message;
    PublishContext<TMessage> IPublishedMessage<TMessage>.Context => _context;
}
