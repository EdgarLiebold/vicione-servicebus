using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using ViciOne.ServiceBus.Util;

namespace ViciOne.ServiceBus.Testing;

/// <summary>
/// Provides a multi test consumer implementation.
/// </summary>
public class MultiTestConsumer
{
    readonly List<IConsumerConfigurator> _configures;
    readonly ReceivedMessageList _received;
    readonly CancellationToken _testCompleted;
    readonly TimeProvider _timeProvider;

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="timeout">The timeout value.</param>
    /// <param name="testCompleted">The test completed value.</param>
    public MultiTestConsumer(TimeSpan timeout, CancellationToken testCompleted = default)
        : this(timeout, TimeProvider.System, testCompleted)
    {
    }

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="timeout">The timeout value.</param>
    /// <param name="timeProvider">The time provider value.</param>
    /// <param name="testCompleted">The test completed value.</param>
    public MultiTestConsumer(TimeSpan timeout, TimeProvider timeProvider, CancellationToken testCompleted = default)
    {
        _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
        _testCompleted = testCompleted;
        Timeout = timeout;
        _configures = new List<IConsumerConfigurator>();

        _received = new ReceivedMessageList(timeout, testCompleted, timeProvider);
    }

    /// <summary>
    /// Gets the received value.
    /// </summary>
    public IReceivedMessageList Received => _received;
    /// <summary>
    /// Gets the timeout value.
    /// </summary>
    public TimeSpan Timeout { get; }

    /// <summary>
    /// Consumes the message provided by the context.
    /// </summary>
    /// <typeparam name="T">The t type.</typeparam>
    /// <returns>The result of the operation.</returns>
    public ReceivedMessageList<T> Consume<T>()
        where T : class
    {
        var consumer = new Of<T>(this);
        var configure = new ConsumerConfigurator<T>(consumer);
        _configures.Add(configure);

        return consumer.Received;
    }

    /// <summary>
    /// Performs the fault operation.
    /// </summary>
    /// <typeparam name="T">The t type.</typeparam>
    /// <returns>The result of the operation.</returns>
    public ReceivedMessageList<T> Fault<T>()
        where T : class
    {
        var consumer = new FaultOf<T>(this);
        var configure = new ConsumerConfigurator<T>(consumer);
        _configures.Add(configure);

        return consumer.Received;
    }

    /// <summary>
    /// Performs the connect operation.
    /// </summary>
    /// <param name="bus">The bus value.</param>
    /// <returns>The result of the operation.</returns>
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

    /// <summary>
    /// Performs the configure operation.
    /// </summary>
    /// <param name="configurator">The configurator value.</param>
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
