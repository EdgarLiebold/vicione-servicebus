using System;

namespace ViciOne.ServiceBus.Configuration;

/// <summary>Configures collection and delivery of messages as bounded batches.</summary>
/// <typeparam name="TMessage">The message contract collected into each batch.</typeparam>
public interface IBatchConfigurator<TMessage> :
    IConsumeConfigurator
    where TMessage : class
{
    /// <summary>Sets the maximum collection interval before a partial batch is delivered.</summary>
    TimeSpan TimeLimit { set; }

    /// <summary>Sets the message arrival from which <see cref="TimeLimit" /> is measured.</summary>
    BatchTimeLimitStart TimeLimitStart { set; }

    /// <summary>Sets the maximum number of messages in one batch.</summary>
    int MessageLimit { set; }

    /// <summary>Sets the maximum number of completed batches delivered concurrently.</summary>
    int ConcurrencyLimit { set; }

    /// <summary>Registers the consumer factory that receives completed batches.</summary>
    /// <typeparam name="TConsumer">The consumer implementation that handles each batch.</typeparam>
    /// <param name="consumerFactory">The factory that supplies consumer instances.</param>
    /// <param name="configure">An optional callback that configures the batch consume pipeline.</param>
    void Consumer<TConsumer>(IConsumerFactory<TConsumer> consumerFactory,
        Action<IConsumerMessageConfigurator<TConsumer, Batch<TMessage>>>? configure = null)
        where TConsumer : class, IConsumer<Batch<TMessage>>;
}
