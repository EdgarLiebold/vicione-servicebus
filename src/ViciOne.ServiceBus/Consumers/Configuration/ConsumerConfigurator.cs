using System;
using System.Collections.Generic;
using System.Linq;

namespace ViciOne.ServiceBus.Configuration;

/// <summary>
/// Provides a consumer configurator implementation.
/// </summary>
/// <typeparam name="TConsumer">The t consumer type.</typeparam>
public class ConsumerConfigurator<TConsumer> :
    IConsumerConfigurator<TConsumer>,
    IReceiveEndpointSpecification
    where TConsumer : class, IConsumer
{
    readonly IConsumerFactory<TConsumer> _consumerFactory;
    readonly IConsumerSpecification<TConsumer> _specification;

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="consumerFactory">The consumer factory value.</param>
    /// <param name="observer">The observer value.</param>
    public ConsumerConfigurator(IConsumerFactory<TConsumer> consumerFactory, IConsumerConfigurationObserver observer)
    {
        _consumerFactory = consumerFactory ?? throw new ArgumentNullException(nameof(consumerFactory));
        ArgumentNullException.ThrowIfNull(observer);

        _specification = ConsumerConnectorCache<TConsumer>.Connector.CreateConsumerSpecification<TConsumer>();

        _specification.ConnectConsumerConfigurationObserver(observer);
    }

    /// <summary>
    /// Adds pipe specification to the configuration.
    /// </summary>
    /// <param name="specification">The specification value.</param>
    public void AddPipeSpecification(IPipeSpecification<ConsumerConsumeContext<TConsumer>> specification)
    {
        ArgumentNullException.ThrowIfNull(specification);

        _specification.AddPipeSpecification(specification);
    }

    /// <summary>
    /// Connects consumer configuration observer.
    /// </summary>
    /// <param name="observer">The observer value.</param>
    /// <returns>The result of the operation.</returns>
    public ConnectHandle ConnectConsumerConfigurationObserver(IConsumerConfigurationObserver observer)
    {
        return _specification.ConnectConsumerConfigurationObserver(observer);
    }

    /// <summary>
    /// Performs the message operation.
    /// </summary>
    /// <typeparam name="T">The t type.</typeparam>
    /// <param name="configure">The configuration callback.</param>
    public void Message<T>(Action<IConsumerMessageConfigurator<T>>? configure)
        where T : class
    {
        _specification.Message(configure);
    }

    /// <summary>
    /// Consumes r message.
    /// </summary>
    /// <typeparam name="T">The t type.</typeparam>
    /// <param name="configure">The configuration callback.</param>
    public void ConsumerMessage<T>(Action<IConsumerMessageConfigurator<TConsumer, T>>? configure)
        where T : class
    {
        _specification.ConsumerMessage(configure);
    }

    /// <summary>
    /// Performs the options operation.
    /// </summary>
    /// <typeparam name="T">The t type.</typeparam>
    /// <param name="configure">The configuration callback.</param>
    /// <returns>The result of the operation.</returns>
    public T Options<T>(Action<T>? configure = null)
        where T : IOptions, new()
    {
        return _specification.Options(configure);
    }

    /// <summary>
    /// Performs the options operation.
    /// </summary>
    /// <typeparam name="T">The t type.</typeparam>
    /// <param name="options">The options value.</param>
    /// <param name="configure">The configuration callback.</param>
    /// <returns>The result of the operation.</returns>
    public T Options<T>(T options, Action<T>? configure = null)
        where T : IOptions
    {
        return _specification.Options(options, configure);
    }

    /// <summary>
    /// Attempts to get options.
    /// </summary>
    /// <typeparam name="T">The t type.</typeparam>
    /// <param name="options">The options value.</param>
    /// <returns><see langword="true" /> when the condition is satisfied; otherwise, <see langword="false" />.</returns>
    public bool TryGetOptions<T>(out T options)
        where T : IOptions
    {
        return _specification.TryGetOptions(out options);
    }

    /// <summary>
    /// Performs the select options operation.
    /// </summary>
    /// <typeparam name="T">The t type.</typeparam>
    /// <returns>The result of the operation.</returns>
    public IEnumerable<T> SelectOptions<T>()
        where T : class
    {
        return _specification.SelectOptions<T>();
    }

    /// <summary>
    /// Gets or sets the concurrent message limit value.
    /// </summary>
    public int? ConcurrentMessageLimit
    {
        set => _specification.ConcurrentMessageLimit = value;
    }

    /// <summary>
    /// Gets or sets the concurrency policy value.
    /// </summary>
    public ConsumerConcurrencyPolicy ConcurrencyPolicy
    {
        set => _specification.ConcurrencyPolicy = value;
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
        _specification.UsePartitionedConcurrency(partitionCount, selector, comparer);
    }

    /// <summary>
    /// Validates the current configuration.
    /// </summary>
    /// <returns>The result of the operation.</returns>
    public IEnumerable<ValidationResult> Validate()
    {
        return _consumerFactory.Validate().Concat(_specification.Validate());
    }

    /// <summary>
    /// Performs the configure operation.
    /// </summary>
    /// <param name="builder">The builder value.</param>
    public void Configure(IReceiveEndpointBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        ConsumerConnectorCache<TConsumer>.Connector.ConnectConsumer(builder, _consumerFactory, _specification);
    }
}
