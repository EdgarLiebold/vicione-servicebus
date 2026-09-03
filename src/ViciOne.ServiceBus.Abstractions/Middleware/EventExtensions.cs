namespace ViciOne.ServiceBus
{
    using System;
    using System.Threading.Tasks;
    using Contracts;
    using Middleware;


    public static class EventExtensions
    {
        public static Task PublishEvent<T>(this IPipe<EventContext> pipe, T message, TimeProvider? timeProvider = null)
            where T : class
        {
            if (pipe == null)
                throw new ArgumentNullException(nameof(pipe));
            if (message == null)
                throw new ArgumentNullException(nameof(message));

            var context = new PublishEventContext<T>(message, timeProvider ?? TimeProvider.System);

            return pipe.Send(context);
        }


        class PublishEventContext<T> :
            BasePipeContext,
            EventContext<T>
            where T : class
        {
            public PublishEventContext(T @event, TimeProvider timeProvider)
            {
                Event = @event;
                Timestamp = timeProvider.GetUtcNow().UtcDateTime;
                this.SetTimeProvider(timeProvider);
            }

            public DateTime Timestamp { get; }

            public T Event { get; }
        }
    }
}
