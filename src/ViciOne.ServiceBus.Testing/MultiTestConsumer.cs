using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using ViciOne.ServiceBus.Util;

namespace ViciOne.ServiceBus.Testing;

/// <summary>Consumes multi test messages.</summary>
public class MultiTestConsumer
{
    readonly List<IConsumerConfigurator> _configures;
    readonly ReceivedMessageList _received;
    readonly CancellationToken _testCompleted;
    readonly TimeProvider _timeProvider;

    /// <summary>Initializes a new instance.</summary>
    /// <param name="timeout">The maximum duration allowed for the operation.</param>
    /// <param name="testCompleted">The test completed.</param>
    public MultiTestConsumer(TimeSpan timeout, CancellationToken testCompleted = default)
        : this(timeout, TimeProvider.System, testCompleted)
    {
    }

    /// <summary>Initializes a new instance.</summary>
    /// <param name="timeout">The maximum duration allowed for the operation.</param>
    /// <param name="timeProvider">The time source used by the operation.</param>
    /// <param name="testCompleted">The test completed.</param>
    public MultiTestConsumer(TimeSpan timeout, TimeProvider timeProvider, CancellationToken testCompleted = default)
    {
        _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
        _testCompleted = testCompleted;
        Timeout = timeout;
        _configures = new List<IConsumerConfigurator>();

        _received = new ReceivedMessageList(timeout, testCompleted, timeProvider);
    }

    /// <summary>Gets the received.</summary>
    public IReceivedMessageList Received => _received;
    /// <summary>Gets the timeout.</summary>
    public TimeSpan Timeout { get; }

    /// <summary>Consumes the message provided by the context.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <returns>The received message list produced by the operation.</returns>
    public ReceivedMessageList<T> Consume<T>()
        where T : class
    {
        var consumer = new Of<T>(this);
        var configure = new ConsumerConfigurator<T>(consumer);
        _configures.Add(configure);

        return consumer.Received;
    }

    /// <summary>Creates or reports a fault.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <returns>The received message list produced by the operation.</returns>
    public ReceivedMessageList<T> Fault<T>()
        where T : class
    {
        var consumer = new FaultOf<T>(this);
        var configure = new ConsumerConfigurator<T>(consumer);
        _configures.Add(configure);

        return consumer.Received;
    }

    /// <summary>Connects the configured observer or endpoint.</summary>
    /// <param name="bus">The bus.</param>
    /// <returns>A handle that disconnects the registration.</returns>
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

    /// <summary>Applies the supplied configuration.</summary>
    /// <param name="configurator">The configurator to update.</param>
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

        public Task ConsumeAsync(ConsumeContext<T> context)
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

        public Task ConsumeAsync(ConsumeContext<T> context)
        {
            Received.Add(context);
            _multiConsumer._received.Add(context);

            throw new InvalidOperationException("This is intentional from a test");
        }
    }
}
