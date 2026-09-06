using System;

namespace ViciOne.ServiceBus.Configuration;

/// <summary>Batching is an experimental feature, and may be changed at any time in the future.</summary>
/// <typeparam name="TMessage">The message contract processed by the member.</typeparam>
public interface IBatchConfigurator<TMessage> :
    IConsumeConfigurator
    where TMessage : class
{
    /// <summary>Set the maximum time to wait for messages before the batch is automatically completed.</summary>
    TimeSpan TimeLimit { set; }

    /// <summary>Sets the starting point for the <see cref="TimeLimit"/>.</summary>
    BatchTimeLimitStart TimeLimitStart { set; }

    /// <summary>Set the maximum number of messages which can be added to a single batch.</summary>
    int MessageLimit { set; }

    /// <summary>Set the maximum number of concurrent batches which can execute at the same time.</summary>
    int ConcurrencyLimit { set; }

    /// <summary>Specify the consumer factory for the batch message consumer.</summary>
    /// <typeparam name="TConsumer">The consumer implementation used by the member.</typeparam>
    /// <param name="consumerFactory">The consumer factory.</param>
    /// <param name="configure">Configure the consumer pipe.</param>
    void Consumer<TConsumer>(IConsumerFactory<TConsumer> consumerFactory,
        Action<IConsumerMessageConfigurator<TConsumer, Batch<TMessage>>>? configure = null)
        where TConsumer : class, IConsumer<Batch<TMessage>>;
}
