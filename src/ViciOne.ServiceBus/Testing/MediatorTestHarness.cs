namespace ViciOne.ServiceBus.Testing
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;
    using Implementations;
    using Mediator;


    public class MediatorTestHarness :
        AsyncTestHarness,
        IBaseTestHarness
    {
        BusTestConsumeObserver _consumed;
        BusTestPublishObserver _published;
        BusTestSendObserver _sent;

        public MediatorTestHarness()
            : this(TimeProvider.System)
        {
        }

        public MediatorTestHarness(TimeProvider timeProvider)
            : base(timeProvider)
        {
            TestInactivityTimeout = TimeSpan.FromSeconds(1);
        }

        public IMediator Mediator { get; private set; }
        public CancellationToken CancellationToken => TestCancellationToken;
        public IReceivedMessageList Consumed => _consumed.Messages;
        public IPublishedMessageList Published => _published.Messages;
        public ISentMessageList Sent => _sent.Messages;

        public event Action<IMediatorConfigurator> OnConfigureMediator;

        public virtual async Task Start()
        {
            _consumed = new BusTestConsumeObserver(TestTimeout, InactivityToken, TimeProvider);
            _consumed.ConnectInactivityObserver(InactivityObserver);

            _published = new BusTestPublishObserver(TestTimeout, TestInactivityTimeout, InactivityToken, TimeProvider);
            _published.ConnectInactivityObserver(InactivityObserver);

            _sent = new BusTestSendObserver(TestTimeout, TestInactivityTimeout, InactivityToken, TimeProvider);
            _sent.ConnectInactivityObserver(InactivityObserver);

            Mediator = CreateMediator();

            Mediator.ConnectConsumeObserver(_consumed);
            Mediator.ConnectPublishObserver(_published);
            Mediator.ConnectSendObserver(_sent);
        }

        protected virtual void ConfigureMediator(IMediatorConfigurator configurator)
        {
            OnConfigureMediator?.Invoke(configurator);
        }

        public override void Dispose()
        {
            _consumed?.Dispose();
            _published?.Dispose();
            _sent?.Dispose();

            base.Dispose();
        }

        public virtual IRequestClient<TRequest> CreateRequestClient<TRequest>()
            where TRequest : class
        {
            return Mediator.CreateRequestClient<TRequest>(TestTimeout);
        }

        IMediator CreateMediator()
        {
            return Bus.Factory.CreateMediator(configurator =>
            {
                ConfigureMediator(configurator);
            });
        }
    }
}
