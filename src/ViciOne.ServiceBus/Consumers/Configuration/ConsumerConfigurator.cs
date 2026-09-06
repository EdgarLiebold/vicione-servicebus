using System;
using System.Collections.Generic;
using System.Linq;

namespace ViciOne.ServiceBus.Configuration;

/// <summary>Configures consumer.</summary>
/// <typeparam name="TConsumer">The consumer implementation used by the member.</typeparam>
public class ConsumerConfigurator<TConsumer> :
    IConsumerConfigurator<TConsumer>,
    IReceiveEndpointSpecification
    where TConsumer : class, IConsumer
{
    readonly IConsumerFactory<TConsumer> _consumerFactory;
    readonly IConsumerSpecification<TConsumer> _specification;

    /// <summary>Initializes a new instance.</summary>
    /// <param name="consumerFactory">The consumer factory.</param>
    /// <param name="observer">The observer to connect.</param>
    public ConsumerConfigurator(IConsumerFactory<TConsumer> consumerFactory, IConsumerConfigurationObserver observer)
    {
        _consumerFactory = consumerFactory ?? throw new ArgumentNullException(nameof(consumerFactory));
        ArgumentNullException.ThrowIfNull(observer);

        _specification = ConsumerConnectorCache<TConsumer>.Connector.CreateConsumerSpecification<TConsumer>();

        _specification.ConnectConsumerConfigurationObserver(observer);
    }

    /// <summary>Adds pipe specification to the configuration.</summary>
    /// <param name="specification">The specification.</param>
    public void AddPipeSpecification(IPipeSpecification<ConsumerConsumeContext<TConsumer>> specification)
    {
        ArgumentNullException.ThrowIfNull(specification);

        _specification.AddPipeSpecification(specification);
    }

    /// <summary>Connects consumer configuration observer.</summary>
    /// <param name="observer">The observer to connect.</param>
    /// <returns>A handle that disconnects the registration.</returns>
    public ConnectHandle ConnectConsumerConfigurationObserver(IConsumerConfigurationObserver observer)
    {
        return _specification.ConnectConsumerConfigurationObserver(observer);
    }

    /// <summary>Applies the message configuration.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="configure">The callback used to configure the component.</param>
    public void Message<T>(Action<IConsumerMessageConfigurator<T>>? configure)
        where T : class
    {
        _specification.Message(configure);
    }

    /// <summary>Consumes r message.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="configure">The callback used to configure the component.</param>
    public void ConsumerMessage<T>(Action<IConsumerMessageConfigurator<TConsumer, T>>? configure)
        where T : class
    {
        _specification.ConsumerMessage(configure);
    }

    /// <summary>Applies the configured options.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="configure">The callback used to configure the component.</param>
    /// <returns>The t produced by the operation.</returns>
    public T Options<T>(Action<T>? configure = null)
        where T : IOptions, new()
    {
        return _specification.Options(configure);
    }

    /// <summary>Applies the configured options.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="options">The options that control the operation.</param>
    /// <param name="configure">The callback used to configure the component.</param>
    /// <returns>The t produced by the operation.</returns>
    public T Options<T>(T options, Action<T>? configure = null)
        where T : IOptions
    {
        return _specification.Options(options, configure);
    }

    /// <summary>Attempts to get options.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="options">Receives the options produced by the operation.</param>
    /// <returns><see langword="true" /> when the condition is satisfied; otherwise, <see langword="false" />.</returns>
    public bool TryGetOptions<T>(out T options)
        where T : IOptions
    {
        return _specification.TryGetOptions(out options);
    }

    /// <summary>Selects options.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <returns>The selected options.</returns>
    public IEnumerable<T> SelectOptions<T>()
        where T : class
    {
        return _specification.SelectOptions<T>();
    }

    /// <summary>Gets or sets the concurrent message limit.</summary>
    public int? ConcurrentMessageLimit
    {
        set => _specification.ConcurrentMessageLimit = value;
    }

    /// <summary>Gets or sets the concurrency policy.</summary>
    public ConsumerConcurrencyPolicy ConcurrencyPolicy
    {
        set => _specification.ConcurrencyPolicy = value;
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
        _specification.UsePartitionedConcurrency(partitionCount, selector, comparer);
    }

    /// <summary>Validates the current configuration.</summary>
    /// <returns>The validation failures.</returns>
    public IEnumerable<ValidationResult> Validate()
    {
        return _consumerFactory.Validate().Concat(_specification.Validate());
    }

    /// <summary>Applies the supplied configuration.</summary>
    /// <param name="builder">The builder that receives the configuration.</param>
    public void Configure(IReceiveEndpointBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        ConsumerConnectorCache<TConsumer>.Connector.ConnectConsumer(builder, _consumerFactory, _specification);
    }
}
