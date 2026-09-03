namespace ViciOne.ServiceBus.Testing
{
    using System;


    public class PublishedMessage<T> :
        IPublishedMessage<T>
        where T : class
    {
        readonly PublishContext<T> _context;

        public PublishedMessage(PublishContext<T> context, Exception exception = null)
            : this(context, exception, TimeProvider.System)
        {
        }

        public PublishedMessage(PublishContext<T> context, Exception exception, TimeProvider timeProvider)
        {
            ArgumentNullException.ThrowIfNull(context);
            ArgumentNullException.ThrowIfNull(timeProvider);

            _context = context;
            Exception = exception;

            ElementId = _context.MessageId;

            var now = timeProvider.GetUtcNow().UtcDateTime;
            StartTime = context.SentTime ?? now;
            ElapsedTime = now - StartTime;
        }

        public Guid? ElementId { get; }
        SendContext IPublishedMessage.Context => _context;
        public DateTime StartTime { get; }
        public TimeSpan ElapsedTime { get; }
        public Exception Exception { get; }
        public Type MessageType => typeof(T);
        public string ShortTypeName => TypeCache<T>.ShortName;
        object IPublishedMessage.MessageObject => _context.Message;
        PublishContext<T> IPublishedMessage<T>.Context => _context;
    }
}
