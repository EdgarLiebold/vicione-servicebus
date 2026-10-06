using System;
using System.Collections.Generic;
using System.Linq;
using ViciOne.ServiceBus.Middleware;

namespace ViciOne.ServiceBus.Configuration;

/// <summary>Owns shared and message-specific pipeline configuration for one consumer.</summary>
/// <typeparam name="TConsumer">The consumer implementation configured by this specification.</typeparam>
public sealed class ConsumerSpecification<TConsumer> :
    OptionsSet,
    IConsumerSpecification<TConsumer>
    where TConsumer : class
{
    readonly ConnectHandle[] _handles;
    readonly IReadOnlyDictionary<Type, IConsumerMessageSpecification<TConsumer>> _messageTypes;
    readonly ConsumerConfigurationObservable _observers;
    readonly ConfigurationObserverNotification _configurationNotification = new ConfigurationObserverNotification();
    readonly HashSet<Type> _partitionedMessageTypes = [];
    ConsumerConcurrencyGate<object>? _concurrencyGate;
    ConsumerConcurrencyPolicy? _concurrencyPolicy;

    /// <summary>Creates a consumer specification from the convention-discovered message contracts.</summary>
    /// <param name="messageSpecifications">The per-message specifications owned by this consumer.</param>
    public ConsumerSpecification(IEnumerable<IConsumerMessageSpecification<TConsumer>> messageSpecifications)
    {
        ArgumentNullException.ThrowIfNull(messageSpecifications);

        _messageTypes = messageSpecifications.ToDictionary(x => x.MessageType);

        _observers = new ConsumerConfigurationObservable();
        var handles = new List<ConnectHandle>(_messageTypes.Count);
        try
        {
            foreach (IConsumerMessageSpecification<TConsumer> specification in _messageTypes.Values)
                handles.Add(specification.ConnectConsumerConfigurationObserver(_observers));
            _handles = handles.ToArray();
        }
        catch (Exception operationFailure)
        {
            try
            {
                new global::ViciOne.ServiceBus.Util.MultipleConnectHandle(handles.Where(handle => handle is not null)).Disconnect();
            }
            catch (Exception cleanupFailure)
            {
                throw new AggregateException("Consumer configuration observer connection and cleanup failed.", operationFailure, cleanupFailure);
            }
            throw;
        }
    }

    /// <summary>Sets the maximum number of messages admitted concurrently for this consumer.</summary>
    public int? ConcurrentMessageLimit
    {
        get => _concurrencyPolicy?.Mode == ConsumerConcurrencyMode.Parallel
            ? _concurrencyPolicy.Concurrency
            : null;
        set
        {
            if (value.HasValue)
                SetConcurrencyPolicy(ConsumerConcurrencyPolicy.Parallel(value.Value));
        }
    }

    /// <summary>Sets the consumer-wide concurrency policy.</summary>
    public ConsumerConcurrencyPolicy ConcurrencyPolicy
    {
        set => SetConcurrencyPolicy(value);
    }

    /// <summary>Applies the message configuration.</summary>
    /// <typeparam name="TMessage">The message contract to configure.</typeparam>
    /// <param name="configure">An optional callback that adds message-level middleware.</param>
    public void Message<TMessage>(Action<IConsumerMessageConfigurator<TMessage>>? configure)
        where TMessage : class
    {
        IConsumerMessageSpecification<TConsumer, TMessage> specification = GetMessageSpecification<TMessage>();

        configure?.Invoke(specification);
    }

    /// <summary>Applies middleware that runs after the consumer instance has been obtained.</summary>
    /// <typeparam name="TMessage">The message contract to configure.</typeparam>
    /// <param name="configure">An optional callback that adds middleware around the obtained consumer instance.</param>
    public void ConsumerMessage<TMessage>(Action<IConsumerMessageConfigurator<TConsumer, TMessage>>? configure)
        where TMessage : class
    {
        IConsumerMessageSpecification<TConsumer, TMessage> specification = GetMessageSpecification<TMessage>();

        configure?.Invoke(specification);
    }

    /// <summary>Gets the specification for a message contract consumed by this consumer.</summary>
    /// <typeparam name="TMessage">The message contract.</typeparam>
    /// <returns>The message specification.</returns>
    public IConsumerMessageSpecification<TConsumer, TMessage> GetMessageSpecification<TMessage>()
        where TMessage : class
    {
        foreach (IConsumerMessageSpecification<TConsumer> messageSpecification in _messageTypes.Values)
        {
            if (messageSpecification.TryGetMessageSpecification(out IConsumerMessageSpecification<TConsumer, TMessage>? result))
                return result;
        }

        throw new ArgumentException($"Message type {TypeCache<TMessage>.ShortName} is not consumed by {TypeCache<TConsumer>.ShortName}.");
    }

    /// <summary>Applies the consumer-wide concurrency policy to a message pipeline.</summary>
    /// <typeparam name="TMessage">The message contract.</typeparam>
    /// <param name="pipeConfigurator">The pipe configurator.</param>
    public void ConfigureMessagePipe<TMessage>(IPipeConfigurator<ConsumeContext<TMessage>> pipeConfigurator)
        where TMessage : class
    {
        ArgumentNullException.ThrowIfNull(pipeConfigurator);

        if (_concurrencyPolicy is null)
            return;

        _concurrencyGate ??= new ConsumerConcurrencyGate<object>(_concurrencyPolicy);
        pipeConfigurator.AddPipeSpecification(new ConsumerConcurrencyPipeSpecification<TMessage>(_concurrencyGate, _concurrencyPolicy));
    }

    /// <summary>Configures partitioned concurrency for the current pipeline.</summary>
    /// <typeparam name="TMessage">The message contract partitioned by the policy.</typeparam>
    /// <typeparam name="TKey">The non-null partition-key type.</typeparam>
    /// <param name="partitionCount">The number of independent ordering partitions.</param>
    /// <param name="selector">The delegate that selects a partition key from each message.</param>
    /// <param name="comparer">An optional equality comparer for partition keys.</param>
    public void UsePartitionedConcurrency<TMessage, TKey>(
        int partitionCount,
        ConsumerPartitionKeySelector<TMessage, TKey> selector,
        IEqualityComparer<TKey>? comparer = null)
        where TMessage : class
        where TKey : notnull
    {
        ArgumentNullException.ThrowIfNull(selector);
        ConsumerConcurrencyPolicy policy = ConsumerConcurrencyPolicy.Partitioned(partitionCount);
        IConsumerMessageSpecification<TConsumer, TMessage> specification = GetMessageSpecification<TMessage>();

        if (_concurrencyPolicy is not null)
        {
            throw new ConfigurationException(
                global::ViciOne.ServiceBus.Providers.Configuration.ConfigurationMessages.Create("Consumer", "unknown", $"Consumer '{TypeCache<TConsumer>.ShortName}' cannot combine a consumer-wide concurrency policy with partitioned message concurrency.", "Correct the named configuration before starting the host"));
        }

        if (!_partitionedMessageTypes.Add(typeof(TMessage)))
        {
            throw new ConfigurationException(
                global::ViciOne.ServiceBus.Providers.Configuration.ConfigurationMessages.Create("Consumer", "unknown", $"Consumer '{TypeCache<TConsumer>.ShortName}' already has a concurrency policy for message '{TypeCache<TMessage>.ShortName}'.", "Correct the named configuration before starting the host"));
        }

        var gate = new PartitionedConsumerConcurrencyGate<TMessage, TKey>(partitionCount, selector, comparer);
        specification.AddPipeSpecification(new ConsumerConcurrencyPipeSpecification<TMessage>(gate, policy));
    }

    /// <summary>Validates the current configuration.</summary>
    /// <returns>The validation failures.</returns>
    public IEnumerable<ValidationResult> Validate()
    {
        _configurationNotification.EnsureNotified(() =>
            _observers.ForEach(observer => observer.ConsumerConfigured(this)));

        return _messageTypes.Values.SelectMany(x => x.Validate())
            .Concat(ValidateOptions())
            .ToArray();
    }

    /// <summary>Adds consumer-wide middleware to every discovered message contract.</summary>
    /// <param name="specification">The consumer-wide middleware specification to project.</param>
    public void AddPipeSpecification(IPipeSpecification<ConsumerConsumeContext<TConsumer>> specification)
    {
        ArgumentNullException.ThrowIfNull(specification);

        foreach (IConsumerMessageSpecification<TConsumer> messageSpecification in _messageTypes.Values)
            messageSpecification.AddPipeSpecification(specification);
    }

    /// <summary>Connects consumer configuration observer.</summary>
    /// <param name="observer">The observer to connect.</param>
    /// <returns>A handle that disconnects the registration.</returns>
    public ConnectHandle ConnectConsumerConfigurationObserver(IConsumerConfigurationObserver observer)
    {
        ArgumentNullException.ThrowIfNull(observer);

        return _observers.Connect(observer);
    }

    private void SetConcurrencyPolicy(ConsumerConcurrencyPolicy policy)
    {
        ArgumentNullException.ThrowIfNull(policy);
        if (policy.Mode == ConsumerConcurrencyMode.Partitioned)
        {
            throw new ArgumentException(
                "Partitioned concurrency requires a strongly typed message and partition-key selector.",
                nameof(policy));
        }

        if (_partitionedMessageTypes.Count > 0)
        {
            throw new ConfigurationException(
                global::ViciOne.ServiceBus.Providers.Configuration.ConfigurationMessages.Create("Consumer", "unknown", $"Consumer '{TypeCache<TConsumer>.ShortName}' cannot combine partitioned message concurrency with a consumer-wide concurrency policy.", "Correct the named configuration before starting the host"));
        }

        if (_concurrencyPolicy is not null && _concurrencyPolicy != policy)
        {
            throw new ConfigurationException(
                global::ViciOne.ServiceBus.Providers.Configuration.ConfigurationMessages.Create("Consumer", "unknown", $"Consumer '{TypeCache<TConsumer>.ShortName}' has conflicting consumer concurrency policies '{_concurrencyPolicy}' and '{policy}'.", "Correct the named configuration before starting the host"));
        }

        _concurrencyPolicy = policy;
    }
}
