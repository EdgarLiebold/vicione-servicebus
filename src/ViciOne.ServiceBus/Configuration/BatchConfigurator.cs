using System;

namespace ViciOne.ServiceBus.Configuration;

/// <summary>Configures how messages of one contract type are collected and delivered as batches.</summary>
/// <typeparam name="TMessage">The message contract collected into each batch.</typeparam>
public sealed class BatchConfigurator<TMessage> :
    IBatchConfigurator<TMessage>
    where TMessage : class
{
    readonly IReceiveEndpointConfigurator _configurator;

    /// <summary>Creates a batch configurator for the supplied receive endpoint.</summary>
    /// <param name="configurator">The receive endpoint that owns the batch consumer.</param>
    public BatchConfigurator(IReceiveEndpointConfigurator configurator)
    {
        _configurator = configurator ?? throw new ArgumentNullException(nameof(configurator));

        ConcurrencyLimit = 1;
        MessageLimit = 10;
        TimeLimit = TimeSpan.FromSeconds(10);
        TimeLimitStart = BatchTimeLimitStart.FromFirst;
    }

    /// <summary>Sets the maximum collection time before a partial batch is delivered.</summary>
    public TimeSpan TimeLimit { private get; set; }
    /// <summary>Sets whether the collection timeout starts with the first or most recent message.</summary>
    public BatchTimeLimitStart TimeLimitStart { private get; set; }
    /// <summary>Sets the maximum number of messages in a batch.</summary>
    public int MessageLimit { private get; set; }
    /// <summary>Sets the maximum number of batches delivered concurrently.</summary>
    public int ConcurrencyLimit { private get; set; }

    /// <summary>Registers the consumer factory that receives completed batches.</summary>
    /// <typeparam name="TConsumer">The consumer implementation that handles each batch.</typeparam>
    /// <param name="consumerFactory">The factory that supplies consumer instances.</param>
    /// <param name="configure">An optional callback that configures the batch consume pipeline.</param>
    public void Consumer<TConsumer>(IConsumerFactory<TConsumer> consumerFactory,
        Action<IConsumerMessageConfigurator<TConsumer, Batch<TMessage>>>? configure)
        where TConsumer : class, IConsumer<Batch<TMessage>>
    {
        ArgumentNullException.ThrowIfNull(consumerFactory);

        var configurator = new ConsumerConfigurator<TConsumer>(consumerFactory, _configurator);

        configurator.Options<BatchOptions>(options => options.SetMessageLimit(MessageLimit).SetTimeLimit(TimeLimit).SetTimeLimitStart(TimeLimitStart)
            .SetConcurrencyLimit(ConcurrencyLimit));

        configurator.ConsumerMessage(configure);

        _configurator.AddEndpointSpecification(configurator);
    }
}
