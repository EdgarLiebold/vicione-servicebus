namespace ViciOne.ServiceBus.Testing.Implementations
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;


    public class BusTestPublishObserver :
        InactivityTestObserver,
        IPublishObserver
    {
        readonly PublishedMessageList _messages;

        public BusTestPublishObserver(TimeSpan timeout, TimeSpan inactivityTimout, CancellationToken testCompleted = default)
            : this(timeout, inactivityTimout, testCompleted, TimeProvider.System)
        {
        }

        public BusTestPublishObserver(TimeSpan timeout, TimeSpan inactivityTimout, CancellationToken testCompleted, TimeProvider timeProvider)
            : base(timeProvider)
        {
            _messages = new PublishedMessageList(timeout, testCompleted, timeProvider);

            StartTimer(inactivityTimout);
        }

        public IPublishedMessageList Messages => _messages;

        Task IPublishObserver.PrePublish<T>(PublishContext<T> context)
        {
            return RestartTimer();
        }

        Task IPublishObserver.PostPublish<T>(PublishContext<T> context)
        {
            _messages.Add(context);

            return RestartTimer(false);
        }

        Task IPublishObserver.PublishFault<T>(PublishContext<T> context, Exception exception)
        {
            _messages.Add(context, exception);

            return RestartTimer(false);
        }
    }
}
