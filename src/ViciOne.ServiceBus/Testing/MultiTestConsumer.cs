namespace ViciOne.ServiceBus.Testing
{
    using System;
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;
    using Util;


    public class MultiTestConsumer
    {
        readonly List<IConsumerConfigurator> _configures;
        readonly ReceivedMessageList _received;
        readonly CancellationToken _testCompleted;
        readonly TimeProvider _timeProvider;

        public MultiTestConsumer(TimeSpan timeout, CancellationToken testCompleted = default)
            : this(timeout, TimeProvider.System, testCompleted)
        {
        }

        public MultiTestConsumer(TimeSpan timeout, TimeProvider timeProvider, CancellationToken testCompleted = default)
        {
            _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
            _testCompleted = testCompleted;
            Timeout = timeout;
            _configures = new List<IConsumerConfigurator>();

            _received = new ReceivedMessageList(timeout, testCompleted, timeProvider);
        }

        public IReceivedMessageList Received => _received;
        public TimeSpan Timeout { get; }

        public ReceivedMessageList<T> Consume<T>()
            where T : class
        {
            var consumer = new Of<T>(this);
            var configure = new ConsumerConfigurator<T>(consumer);
            _configures.Add(configure);

            return consumer.Received;
        }

        public ReceivedMessageList<T> Fault<T>()
            where T : class
        {
            var consumer = new FaultOf<T>(this);
            var configure = new ConsumerConfigurator<T>(consumer);
            _configures.Add(configure);

            return consumer.Received;
        }

        public ConnectHandle Connect(IConsumePipeConnector bus)
        {
            var handles = new List<ConnectHandle>(_configures.Count);
            try
            {
                foreach (var configure in _configures)
                {
                    var handle = configure.Connect(bus);

                    handles.Add(handle);
                }

                return new MultipleConnectHandle(handles);
            }
            catch (Exception)
            {
                foreach (var handle in handles)
                    handle.Dispose();
                throw;
            }
        }

        public void Configure(IReceiveEndpointConfigurator configurator)
        {
            foreach (var configure in _configures)
                configure.Configure(configurator);
        }


        class ConsumerConfigurator<T> :
            IConsumerConfigurator
            where T : class
        {
            readonly IConsumer<T> _consumer;

            public ConsumerConfigurator(IConsumer<T> consumer)
            {
                _consumer = consumer;
            }

            public ConnectHandle Connect(IConsumePipeConnector bus)
            {
                return bus.ConnectInstance(_consumer);
            }

            public void Configure(IReceiveEndpointConfigurator configurator)
            {
                configurator.Instance(_consumer);
            }
        }


        interface IConsumerConfigurator
        {
            ConnectHandle Connect(IConsumePipeConnector bus);
            void Configure(IReceiveEndpointConfigurator configurator);
        }


        class Of<T> :
            IConsumer<T>
            where T : class
        {
            readonly MultiTestConsumer _multiConsumer;

            public Of(MultiTestConsumer multiConsumer)
            {
                _multiConsumer = multiConsumer;
                Received = new ReceivedMessageList<T>(multiConsumer.Timeout, multiConsumer._testCompleted, multiConsumer._timeProvider);
            }

            public ReceivedMessageList<T> Received { get; }

            public Task Consume(ConsumeContext<T> context)
            {
                Received.Add(context);
                _multiConsumer._received.Add(context);

                return Task.CompletedTask;
            }
        }


        class FaultOf<T> :
            IConsumer<T>
            where T : class
        {
            readonly MultiTestConsumer _multiConsumer;

            public FaultOf(MultiTestConsumer multiConsumer)
            {
                _multiConsumer = multiConsumer;
                Received = new ReceivedMessageList<T>(multiConsumer.Timeout, multiConsumer._testCompleted, multiConsumer._timeProvider);
            }

            public ReceivedMessageList<T> Received { get; }

            public Task Consume(ConsumeContext<T> context)
            {
                Received.Add(context);
                _multiConsumer._received.Add(context);

                throw new InvalidOperationException("This is intentional from a test");
            }
        }
    }
}
