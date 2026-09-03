namespace ViciOne.ServiceBus.Testing.Implementations
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;


    public class BusTestSendObserver :
        InactivityTestObserver,
        ISendObserver
    {
        readonly SentMessageList _messages;

        public BusTestSendObserver(TimeSpan timeout, TimeSpan inactivityTimout, CancellationToken testCompleted = default)
            : this(timeout, inactivityTimout, testCompleted, TimeProvider.System)
        {
        }

        public BusTestSendObserver(TimeSpan timeout, TimeSpan inactivityTimout, CancellationToken testCompleted, TimeProvider timeProvider)
            : base(timeProvider)
        {
            _messages = new SentMessageList(timeout, testCompleted, timeProvider);

            StartTimer(inactivityTimout);
        }

        public ISentMessageList Messages => _messages;

        public Task PreSend<T>(SendContext<T> context)
            where T : class
        {
            return RestartTimer();
        }

        public Task PostSend<T>(SendContext<T> context)
            where T : class
        {
            _messages.Add(context);

            return RestartTimer(false);
        }

        public Task SendFault<T>(SendContext<T> context, Exception exception)
            where T : class
        {
            _messages.Add(context, exception);

            return RestartTimer(false);
        }
    }
}
