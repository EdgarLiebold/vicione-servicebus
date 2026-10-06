using System;
using System.Collections.Generic;
using System.Linq;

namespace ViciOne.ServiceBus.Configuration;

/// <summary>Configures and attaches one factory-created consumer to a receive endpoint.</summary>
/// <typeparam name="TConsumer">The consumer implementation attached to the receive endpoint.</typeparam>
public sealed class ConsumerConfigurator<TConsumer> :
    IConsumerConfigurator<TConsumer>,
    IReceiveEndpointSpecification
    where TConsumer : class, IConsumer
{
    readonly IConsumerFactory<TConsumer> _consumerFactory;
    readonly IConsumerSpecification<TConsumer> _specification;

    /// <summary>Creates endpoint configuration for a factory-created consumer.</summary>
    /// <param name="consumerFactory">The factory that supplies a consumer for each delivery.</param>
    /// <param name="observer">The endpoint observer notified as consumer configuration is completed.</param>
    public ConsumerConfigurator(IConsumerFactory<TConsumer> consumerFactory, IConsumerConfigurationObserver observer)
    {
        _consumerFactory = consumerFactory ?? throw new ArgumentNullException(nameof(consumerFactory));
        ArgumentNullException.ThrowIfNull(observer);

        _specification = ConsumerConnectorCache<TConsumer>.Connector.CreateConsumerSpecification<TConsumer>();

        _specification.ConnectConsumerConfigurationObserver(observer);
    }

    /// <summary>Adds consumer-wide middleware to every discovered message contract.</summary>
    /// <param name="specification">The consumer-wide middleware specification to project.</param>
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
        ArgumentNullException.ThrowIfNull(observer);

        return _specification.ConnectConsumerConfigurationObserver(observer);
    }

    /// <summary>Applies the message configuration.</summary>
    /// <typeparam name="TMessage">The message contract to configure.</typeparam>
    /// <param name="configure">An optional callback that adds message-level middleware.</param>
    public void Message<TMessage>(Action<IConsumerMessageConfigurator<TMessage>>? configure)
        where TMessage : class
    {
        _specification.Message(configure);
    }

    /// <summary>Applies middleware that runs after the consumer instance has been obtained.</summary>
    /// <typeparam name="TMessage">The message contract to configure.</typeparam>
    /// <param name="configure">An optional callback that adds middleware around the obtained consumer instance.</param>
    public void ConsumerMessage<TMessage>(Action<IConsumerMessageConfigurator<TConsumer, TMessage>>? configure)
        where TMessage : class
    {
        _specification.ConsumerMessage(configure);
    }

    /// <summary>Applies the configured options.</summary>
    /// <typeparam name="TOptions">The options type.</typeparam>
    /// <param name="configure">An optional callback applied to the newly created options.</param>
    /// <returns>The configured options instance.</returns>
    public TOptions Options<TOptions>(Action<TOptions>? configure = null)
        where TOptions : IOptions, new()
    {
        return _specification.Options(configure);
    }

    /// <summary>Applies the configured options.</summary>
    /// <typeparam name="TOptions">The options type.</typeparam>
    /// <param name="options">The options that control the operation.</param>
    /// <param name="configure">An optional callback applied after the options are added.</param>
    /// <returns>The configured options instance.</returns>
    public TOptions Options<TOptions>(TOptions options, Action<TOptions>? configure = null)
        where TOptions : IOptions
    {
        return _specification.Options(options, configure);
    }

    /// <summary>Attempts to get the configured options of the requested type.</summary>
    /// <typeparam name="TOptions">The options type.</typeparam>
    /// <param name="options">Receives the configured options when present.</param>
    /// <returns><see langword="true" /> when options of the requested type are configured.</returns>
    public bool TryGetOptions<TOptions>(out TOptions options)
        where TOptions : IOptions
    {
        return _specification.TryGetOptions(out options);
    }

    /// <summary>Selects options.</summary>
    /// <typeparam name="TOptions">The options type.</typeparam>
    /// <returns>The selected options.</returns>
    public IEnumerable<TOptions> SelectOptions<TOptions>()
        where TOptions : class
    {
        return _specification.SelectOptions<TOptions>();
    }

    /// <summary>Sets the maximum number of messages admitted concurrently for this consumer.</summary>
    public int? ConcurrentMessageLimit
    {
        set => _specification.ConcurrentMessageLimit = value;
    }

    /// <summary>Sets the consumer-wide concurrency policy.</summary>
    public ConsumerConcurrencyPolicy ConcurrencyPolicy
    {
        set => _specification.ConcurrencyPolicy = value;
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
