using System;

namespace ViciOne.ServiceBus.Testing;

public class ReceivedMessage<T> :
    IReceivedMessage<T>
    where T : class
{
    readonly ConsumeContext<T> _context;
    readonly Exception? _exception;

    public ReceivedMessage(ConsumeContext<T> context, Exception? exception = null)
        : this(context, exception, TimeProvider.System)
    {
    }

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

    public Guid? ElementId { get; }
    ConsumeContext IReceivedMessage.Context => _context.Advanced();
    public DateTimeOffset StartTime { get; }
    public TimeSpan ElapsedTime { get; }
    Exception? IReceivedMessage.Exception => _exception;
    Type IReceivedMessage.MessageType => typeof(T);
    string IReceivedMessage.ShortTypeName => TypeCache<T>.ShortName;
    object IReceivedMessage.MessageObject => _context.Message;
    ConsumeContext<T> IReceivedMessage<T>.Context => _context;
}
