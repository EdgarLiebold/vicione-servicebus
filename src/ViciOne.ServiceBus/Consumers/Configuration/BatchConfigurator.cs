using System;

#nullable enable
namespace ViciOne.ServiceBus.Configuration;

/// <summary>
/// Provides a batch configurator implementation.
/// </summary>
/// <typeparam name="TMessage">The t message type.</typeparam>
public class BatchConfigurator<TMessage> :
    IBatchConfigurator<TMessage>
    where TMessage : class
{
    readonly IReceiveEndpointConfigurator _configurator;

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="configurator">The configurator value.</param>
    public BatchConfigurator(IReceiveEndpointConfigurator configurator)
    {
        _configurator = configurator;

        ConcurrencyLimit = 1;
        MessageLimit = 10;
        TimeLimit = TimeSpan.FromSeconds(10);
        TimeLimitStart = BatchTimeLimitStart.FromFirst;
    }

    /// <summary>
    /// Gets or sets the time limit value.
    /// </summary>
    public TimeSpan TimeLimit { private get; set; }
    /// <summary>
    /// Gets or sets the time limit start value.
    /// </summary>
    public BatchTimeLimitStart TimeLimitStart { private get; set; }
    /// <summary>
    /// Gets or sets the message limit value.
    /// </summary>
    public int MessageLimit { private get; set; }
    /// <summary>
    /// Gets or sets the concurrency limit value.
    /// </summary>
    public int ConcurrencyLimit { private get; set; }

    /// <summary>
    /// Consumes r.
    /// </summary>
    /// <typeparam name="TConsumer">The t consumer type.</typeparam>
    /// <param name="consumerFactory">The consumer factory value.</param>
    /// <param name="configure">The configuration callback.</param>
    public void Consumer<TConsumer>(IConsumerFactory<TConsumer> consumerFactory,
        Action<IConsumerMessageConfigurator<TConsumer, Batch<TMessage>>>? configure)
        where TConsumer : class, IConsumer<Batch<TMessage>>
    {
        var configurator = new ConsumerConfigurator<TConsumer>(consumerFactory, _configurator);
        configurator.ConnectConsumerConfigurationObserver(_configurator);

        configurator.Options<BatchOptions>(options => options.SetMessageLimit(MessageLimit).SetTimeLimit(TimeLimit).SetTimeLimitStart(TimeLimitStart)
            .SetConcurrencyLimit(ConcurrencyLimit));

        configurator.ConsumerMessage(configure);

        _configurator.AddEndpointSpecification(configurator);
    }
}
