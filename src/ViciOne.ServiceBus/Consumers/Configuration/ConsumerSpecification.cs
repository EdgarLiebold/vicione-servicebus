using System;
using System.Collections.Generic;
using System.Linq;
using ViciOne.ServiceBus.Middleware;

#nullable enable
namespace ViciOne.ServiceBus.Configuration;

/// <summary>
/// Provides a consumer specification implementation.
/// </summary>
/// <typeparam name="TConsumer">The t consumer type.</typeparam>
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

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="messageSpecifications">The message specifications value.</param>
    public ConsumerSpecification(IEnumerable<IConsumerMessageSpecification<TConsumer>> messageSpecifications)
    {
        _messageTypes = messageSpecifications.ToDictionary(x => x.MessageType);

        _observers = new ConsumerConfigurationObservable();
        _handles = _messageTypes.Values.Select(x => x.ConnectConsumerConfigurationObserver(_observers)).ToArray();
    }

    /// <summary>
    /// Gets or sets the concurrent message limit value.
    /// </summary>
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

    /// <summary>
    /// Gets or sets the concurrency policy value.
    /// </summary>
    public ConsumerConcurrencyPolicy ConcurrencyPolicy
    {
        set => SetConcurrencyPolicy(value);
    }

    /// <summary>
    /// Performs the message operation.
    /// </summary>
    /// <typeparam name="T">The t type.</typeparam>
    /// <param name="configure">The configuration callback.</param>
    public void Message<T>(Action<IConsumerMessageConfigurator<T>>? configure)
        where T : class
    {
        IConsumerMessageSpecification<TConsumer, T> specification = GetMessageSpecification<T>();

        configure?.Invoke(specification);
    }

    /// <summary>
    /// Consumes r message.
    /// </summary>
    /// <typeparam name="T">The t type.</typeparam>
    /// <param name="configure">The configuration callback.</param>
    public void ConsumerMessage<T>(Action<IConsumerMessageConfigurator<TConsumer, T>>? configure)
        where T : class
    {
        IConsumerMessageSpecification<TConsumer, T> specification = GetMessageSpecification<T>();

        configure?.Invoke(specification);
    }

    /// <summary>
    /// Gets message specification.
    /// </summary>
    /// <typeparam name="T">The t type.</typeparam>
    /// <returns>The result of the operation.</returns>
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

    /// <summary>
    /// Configures message pipe.
    /// </summary>
    /// <typeparam name="T">The t type.</typeparam>
    /// <param name="pipeConfigurator">The pipe configurator value.</param>
    public void ConfigureMessagePipe<T>(IPipeConfigurator<ConsumeContext<T>> pipeConfigurator)
        where T : class
    {
        if (_concurrencyPolicy is null)
            return;

        _concurrencyGate ??= new ConsumerConcurrencyGate<object>(_concurrencyPolicy);
        pipeConfigurator.AddPipeSpecification(new ConsumerConcurrencyPipeSpecification<T>(_concurrencyGate, _concurrencyPolicy));
    }

    /// <summary>
    /// Configures partitioned concurrency for the current pipeline.
    /// </summary>
    /// <typeparam name="TMessage">The t message type.</typeparam>
    /// <typeparam name="TKey">The t key type.</typeparam>
    /// <param name="partitionCount">The partition count value.</param>
    /// <param name="selector">The selector value.</param>
    /// <param name="comparer">The comparer value.</param>
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
                $"Consumer '{TypeCache<TConsumer>.ShortName}' cannot combine a consumer-wide concurrency policy with partitioned message concurrency.");
        }

        if (!_partitionedMessageTypes.Add(typeof(TMessage)))
        {
            throw new ConfigurationException(
                $"Consumer '{TypeCache<TConsumer>.ShortName}' already has a concurrency policy for message '{TypeCache<TMessage>.ShortName}'.");
        }

        var gate = new PartitionedConsumerConcurrencyGate<TMessage, TKey>(partitionCount, selector, comparer);
        specification.AddPipeSpecification(new ConsumerConcurrencyPipeSpecification<TMessage>(gate, policy));
    }

    /// <summary>
    /// Validates the current configuration.
    /// </summary>
    /// <returns>The result of the operation.</returns>
    public IEnumerable<ValidationResult> Validate()
    {
        _configurationNotification.EnsureNotified(() =>
            _observers.ForEach(observer => observer.ConsumerConfigured(this)));

        return _messageTypes.Values.SelectMany(x => x.Validate())
            .Concat(ValidateOptions())
            .ToArray();
    }

    /// <summary>
    /// Adds pipe specification to the configuration.
    /// </summary>
    /// <param name="specification">The specification value.</param>
    public void AddPipeSpecification(IPipeSpecification<ConsumerConsumeContext<TConsumer>> specification)
    {
        foreach (IConsumerMessageSpecification<TConsumer> messageSpecification in _messageTypes.Values)
            messageSpecification.AddPipeSpecification(specification);
    }

    /// <summary>
    /// Connects consumer configuration observer.
    /// </summary>
    /// <param name="observer">The observer value.</param>
    /// <returns>The result of the operation.</returns>
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
                $"Consumer '{TypeCache<TConsumer>.ShortName}' cannot combine partitioned message concurrency with a consumer-wide concurrency policy.");
        }

        if (_concurrencyPolicy is not null && _concurrencyPolicy != policy)
        {
            throw new ConfigurationException(
                $"Consumer '{TypeCache<TConsumer>.ShortName}' has conflicting consumer concurrency policies '{_concurrencyPolicy}' and '{policy}'.");
        }

        _concurrencyPolicy = policy;
    }
}
