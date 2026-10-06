using System;
using System.Collections.Generic;
namespace ViciOne.ServiceBus.Configuration;

/// <summary>Configures concurrency and observation for a consumer.</summary>
public interface IConsumerConfigurator :
    IConsumeConfigurator,
    IConsumerConfigurationObserverConnector
{
    /// <summary>Sets the maximum number of messages this consumer may process concurrently.</summary>
    int? ConcurrentMessageLimit { set; }

    /// <summary>
    /// Sets the single consumer-local serial or fixed-parallel execution policy. Endpoint
    /// transport concurrency is configured separately on the receive endpoint.
    /// </summary>
    ConsumerConcurrencyPolicy ConcurrencyPolicy { set; }
}


/// <summary>Configures a consumer and its message-specific middleware pipelines.</summary>
/// <typeparam name="TConsumer">The consumer implementation.</typeparam>
public interface IConsumerConfigurator<TConsumer> :
    IPipeConfigurator<ConsumerConsumeContext<TConsumer>>,
    IConsumerConfigurator,
    IOptionsSet
    where TConsumer : class
{
    /// <summary>Configures middleware invoked before the consumer instance is obtained.</summary>
    /// <typeparam name="TMessage">The message contract to configure.</typeparam>
    /// <param name="configure">The callback to configure the message pipeline.</param>
    void Message<TMessage>(Action<IConsumerMessageConfigurator<TMessage>>? configure = null)
        where TMessage : class;

    /// <summary>
    /// Configures message-specific middleware invoked after the consumer instance is obtained.
    /// </summary>
    /// <typeparam name="TMessage">The message contract to configure.</typeparam>
    /// <param name="configure">The callback to configure the message pipeline.</param>
    void ConsumerMessage<TMessage>(Action<IConsumerMessageConfigurator<TConsumer, TMessage>>? configure = null)
        where TMessage : class;

    /// <summary>
    /// Applies bounded, strongly typed partition mutual exclusion to one message contract
    /// consumed by this consumer.
    /// </summary>
    /// <typeparam name="TMessage">The message contract partitioned by the policy.</typeparam>
    /// <typeparam name="TKey">The non-null partition-key type.</typeparam>
    /// <param name="partitionCount">The fixed number of mutual-exclusion partitions.</param>
    /// <param name="selector">The function that selects a partition key from each message context.</param>
    /// <param name="comparer">An optional equality comparer for partition keys.</param>
    void UsePartitionedConcurrency<TMessage, TKey>(
        int partitionCount,
        ConsumerPartitionKeySelector<TMessage, TKey> selector,
        IEqualityComparer<TKey>? comparer = null)
        where TMessage : class
        where TKey : notnull;
}
