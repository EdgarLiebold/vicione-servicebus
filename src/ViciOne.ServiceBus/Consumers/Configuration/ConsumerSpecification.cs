using System;
using System.Collections.Generic;
using System.Linq;
using ViciOne.ServiceBus.Middleware;

namespace ViciOne.ServiceBus.Configuration;

/// <summary>Describes requirements for consumer.</summary>
/// <typeparam name="TConsumer">The consumer implementation used by the member.</typeparam>
public class ConsumerSpecification<TConsumer> :
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

    /// <summary>Initializes a new instance.</summary>
    /// <param name="messageSpecifications">The message specifications.</param>
    public ConsumerSpecification(IEnumerable<IConsumerMessageSpecification<TConsumer>> messageSpecifications)
    {
        _messageTypes = messageSpecifications.ToDictionary(x => x.MessageType);

        _observers = new ConsumerConfigurationObservable();
        _handles = _messageTypes.Values.Select(x => x.ConnectConsumerConfigurationObserver(_observers)).ToArray();
    }

    /// <summary>Gets or sets the concurrent message limit.</summary>
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

    /// <summary>Gets or sets the concurrency policy.</summary>
    public ConsumerConcurrencyPolicy ConcurrencyPolicy
    {
        set => SetConcurrencyPolicy(value);
    }

    /// <summary>Applies the message configuration.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="configure">The callback used to configure the component.</param>
    public void Message<T>(Action<IConsumerMessageConfigurator<T>>? configure)
        where T : class
    {
        IConsumerMessageSpecification<TConsumer, T> specification = GetMessageSpecification<T>();

        configure?.Invoke(specification);
    }

    /// <summary>Consumes r message.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="configure">The callback used to configure the component.</param>
    public void ConsumerMessage<T>(Action<IConsumerMessageConfigurator<TConsumer, T>>? configure)
        where T : class
    {
        IConsumerMessageSpecification<TConsumer, T> specification = GetMessageSpecification<T>();

        configure?.Invoke(specification);
    }

    /// <summary>Gets message specification.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <returns>The message specification.</returns>
    public IConsumerMessageSpecification<TConsumer, T> GetMessageSpecification<T>()
        where T : class
    {
        foreach (IConsumerMessageSpecification<TConsumer> messageSpecification in _messageTypes.Values)
        {
            if (messageSpecification.TryGetMessageSpecification(out IConsumerMessageSpecification<TConsumer, T>? result))
                return result;
        }

        throw new ArgumentException($"MessageType {TypeCache<T>.ShortName} is not consumed by {TypeCache<TConsumer>.ShortName}");
    }

    /// <summary>Configures message pipe.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="pipeConfigurator">The pipe configurator.</param>
    public void ConfigureMessagePipe<T>(IPipeConfigurator<ConsumeContext<T>> pipeConfigurator)
        where T : class
    {
        if (_concurrencyPolicy is null)
            return;

        _concurrencyGate ??= new ConsumerConcurrencyGate<object>(_concurrencyPolicy);
        pipeConfigurator.AddPipeSpecification(new ConsumerConcurrencyPipeSpecification<T>(_concurrencyGate, _concurrencyPolicy));
    }

    /// <summary>Configures partitioned concurrency for the current pipeline.</summary>
    /// <typeparam name="TMessage">The message contract processed by the member.</typeparam>
    /// <typeparam name="TKey">The key used for lookup.</typeparam>
    /// <param name="partitionCount">The partition count.</param>
    /// <param name="selector">The selector.</param>
    /// <param name="comparer">The comparer.</param>
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

    /// <summary>Adds pipe specification to the configuration.</summary>
    /// <param name="specification">The specification.</param>
    public void AddPipeSpecification(IPipeSpecification<ConsumerConsumeContext<TConsumer>> specification)
    {
        foreach (IConsumerMessageSpecification<TConsumer> messageSpecification in _messageTypes.Values)
            messageSpecification.AddPipeSpecification(specification);
    }

    /// <summary>Connects consumer configuration observer.</summary>
    /// <param name="observer">The observer to connect.</param>
    /// <returns>A handle that disconnects the registration.</returns>
    public ConnectHandle ConnectConsumerConfigurationObserver(IConsumerConfigurationObserver observer)
    {
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
