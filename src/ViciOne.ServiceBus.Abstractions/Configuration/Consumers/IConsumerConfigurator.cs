using System;
using System.Collections.Generic;
using ViciOne.ServiceBus.Configuration;

namespace ViciOne.ServiceBus;

public interface IConsumerConfigurator :
    IConsumeConfigurator,
    IConsumerConfigurationObserverConnector
{
    int? ConcurrentMessageLimit { set; }

    /// <summary>
    /// Sets the single consumer-local serial or fixed-parallel execution policy. Endpoint
    /// transport concurrency is configured separately on the receive endpoint.
    /// </summary>
    ConsumerConcurrencyPolicy ConcurrencyPolicy { set; }
}


public interface IConsumerConfigurator<TConsumer> :
    IPipeConfigurator<ConsumerConsumeContext<TConsumer>>,
    IConsumerConfigurator,
    IOptionsSet
    where TConsumer : class
{
    /// <summary>
    /// Add middleware to the message pipeline, which is invoked prior to the consumer factory.
    /// </summary>
    /// <typeparam name="T">The message type</typeparam>
    /// <param name="configure">The callback to configure the message pipeline</param>
    void Message<T>(Action<IConsumerMessageConfigurator<T>>? configure = null)
        where T : class;

    /// <summary>
    /// Add middleware to the consumer pipeline, for the specified message type, which is invoked
    /// after the consumer factory.
    /// </summary>
    /// <typeparam name="T">The message type</typeparam>
    /// <param name="configure">The callback to configure the message pipeline</param>
    void ConsumerMessage<T>(Action<IConsumerMessageConfigurator<TConsumer, T>>? configure = null)
        where T : class;

    /// <summary>
    /// Applies bounded, strongly typed partition mutual exclusion to one message contract
    /// consumed by this consumer.
    /// </summary>
    void UsePartitionedConcurrency<TMessage, TKey>(
        int partitionCount,
        ConsumerPartitionKeySelector<TMessage, TKey> selector,
        IEqualityComparer<TKey>? comparer = null)
        where TMessage : class
        where TKey : notnull;
}
