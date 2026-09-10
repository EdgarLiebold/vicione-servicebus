using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using ViciOne.ServiceBus.Util;

namespace ViciOne.ServiceBus.Testing;

/// <summary>Configures successful and faulting consumers for multiple message contracts and records their deliveries.</summary>
public sealed class MultiTestConsumer
{
    readonly List<IConsumerConfigurator> _configures;
    readonly ConsumedMessageList _consumed;
    readonly CancellationToken _testCancellationToken;
    readonly TimeProvider _timeProvider;

    /// <summary>Creates a multi-message consumer set that uses the system time provider.</summary>
    /// <param name="timeout">The maximum wait for a matching delivery.</param>
    /// <param name="testCancellationToken">The token that cancels pending observation.</param>
    public MultiTestConsumer(TimeSpan timeout, CancellationToken testCancellationToken = default)
        : this(timeout, TimeProvider.System, testCancellationToken)
    {
    }

    /// <summary>Creates a multi-message consumer set.</summary>
    /// <param name="timeout">The maximum wait for a matching delivery.</param>
    /// <param name="timeProvider">The time provider used by observation timeouts.</param>
    /// <param name="testCancellationToken">The token that cancels pending observation.</param>
    public MultiTestConsumer(TimeSpan timeout, TimeProvider timeProvider, CancellationToken testCancellationToken = default)
    {
        _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
        _testCancellationToken = testCancellationToken;
        Timeout = timeout;
        _configures = new List<IConsumerConfigurator>();

        _consumed = new ConsumedMessageList(timeout, testCancellationToken, timeProvider);
    }

    /// <summary>Gets all deliveries observed by the configured consumers.</summary>
    public IConsumedMessageList Consumed => _consumed;
    /// <summary>Gets the maximum wait for a matching delivery.</summary>
    public TimeSpan Timeout { get; }

    /// <summary>Adds a successful consumer for a message contract.</summary>
    /// <typeparam name="TMessage">The consumed message contract.</typeparam>
    /// <returns>The deliveries observed for the message contract.</returns>
    public IConsumedMessageList<TMessage> AddConsumer<TMessage>()
        where TMessage : class
    {
        var consumer = new RecordingConsumer<TMessage>(this);
        var configure = new ConsumerConfigurator<TMessage>(consumer);
        _configures.Add(configure);

        return consumer.Consumed;
    }

    /// <summary>Adds a consumer that records each delivery and then faults.</summary>
    /// <typeparam name="TMessage">The consumed message contract.</typeparam>
    /// <returns>The deliveries observed for the message contract.</returns>
    public IConsumedMessageList<TMessage> AddFaultingConsumer<TMessage>()
        where TMessage : class
    {
        var consumer = new FaultingConsumer<TMessage>(this);
        var configure = new ConsumerConfigurator<TMessage>(consumer);
        _configures.Add(configure);

        return consumer.Consumed;
    }

    /// <summary>Connects every configured consumer to a consume pipe.</summary>
    /// <param name="connector">The consume-pipe connector.</param>
    /// <returns>A handle that disconnects the registration.</returns>
    public ConnectHandle Connect(IConsumePipeConnector connector)
    {
        ArgumentNullException.ThrowIfNull(connector);
        var handles = new List<ConnectHandle>(_configures.Count);
        try
        {
            foreach (var configure in _configures)
            {
                var handle = configure.Connect(connector);

                handles.Add(handle);
            }

            return new MultipleConnectHandle(handles);
        }
        catch
        {
            foreach (var handle in handles)
                handle.Dispose();
            throw;
        }
    }

    /// <summary>Adds every configured consumer to a receive endpoint.</summary>
    /// <param name="configurator">The receive-endpoint configurator.</param>
    public void Configure(IReceiveEndpointConfigurator configurator)
    {
        ArgumentNullException.ThrowIfNull(configurator);
        foreach (var configure in _configures)
            configure.Configure(configurator);
    }

    sealed class ConsumerConfigurator<TMessage> :
        IConsumerConfigurator
        where TMessage : class
    {
        readonly IConsumer<TMessage> _consumer;

        public ConsumerConfigurator(IConsumer<TMessage> consumer)
        {
            _consumer = consumer ?? throw new ArgumentNullException(nameof(consumer));
        }

        public ConnectHandle Connect(IConsumePipeConnector connector)
        {
            return connector.ConnectInstance(_consumer);
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
    sealed class RecordingConsumer<TMessage> :
        IConsumer<TMessage>
        where TMessage : class
    {
        readonly MultiTestConsumer _multiConsumer;

        public RecordingConsumer(MultiTestConsumer multiConsumer)
        {
            _multiConsumer = multiConsumer ?? throw new ArgumentNullException(nameof(multiConsumer));
            Consumed = new ConsumedMessageList<TMessage>(
                multiConsumer.Timeout,
                multiConsumer._testCancellationToken,
                multiConsumer._timeProvider);
        }

        public ConsumedMessageList<TMessage> Consumed { get; }

        public Task ConsumeAsync(ConsumeContext<TMessage> context)
        {
            ArgumentNullException.ThrowIfNull(context);
            Consumed.Add(context);
            _multiConsumer._consumed.Add(context);

            return Task.CompletedTask;
        }
    }
    sealed class FaultingConsumer<TMessage> :
        IConsumer<TMessage>
        where TMessage : class
    {
        readonly MultiTestConsumer _multiConsumer;

        public FaultingConsumer(MultiTestConsumer multiConsumer)
        {
            _multiConsumer = multiConsumer ?? throw new ArgumentNullException(nameof(multiConsumer));
            Consumed = new ConsumedMessageList<TMessage>(
                multiConsumer.Timeout,
                multiConsumer._testCancellationToken,
                multiConsumer._timeProvider);
        }

        public ConsumedMessageList<TMessage> Consumed { get; }

        public Task ConsumeAsync(ConsumeContext<TMessage> context)
        {
            ArgumentNullException.ThrowIfNull(context);
            Consumed.Add(context);
            _multiConsumer._consumed.Add(context);

            return Task.FromException(new InvalidOperationException("The configured test consumer faulted intentionally."));
        }
    }
}
