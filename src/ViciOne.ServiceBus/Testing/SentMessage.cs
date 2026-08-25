namespace ViciOne.ServiceBus.Testing
{
    using System;


    public class SentMessage<T> :
        ISentMessage<T>
        where T : class
    {
        readonly SendContext<T> _context;
        readonly Exception _exception;

        public SentMessage(SendContext<T> context, Exception exception = null)
            : this(context, exception, TimeProvider.System)
        {
        }

        public SentMessage(SendContext<T> context, Exception exception, TimeProvider timeProvider)
        {
            ArgumentNullException.ThrowIfNull(context);
            ArgumentNullException.ThrowIfNull(timeProvider);

            _context = context;
            _exception = exception;

            ElementId = _context.MessageId;

            var now = timeProvider.GetUtcNow().UtcDateTime;
            StartTime = context.SentTime ?? now;
            ElapsedTime = now - StartTime;
        }

        public Guid? ElementId { get; }
        SendContext ISentMessage.Context => _context;
        public DateTime StartTime { get; }
        public TimeSpan ElapsedTime { get; }
        object ISentMessage.MessageObject => _context.Message;
        Exception ISentMessage.Exception => _exception;
        Type ISentMessage.MessageType => typeof(T);
        public string ShortTypeName => TypeCache<T>.ShortName;
        SendContext<T> ISentMessage<T>.Context => _context;
    }
}
