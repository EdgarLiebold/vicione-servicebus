using System;

namespace ViciOne.ServiceBus.Configuration;

/// <summary>Configures batch.</summary>
/// <typeparam name="TMessage">The message contract processed by the member.</typeparam>
public class BatchConfigurator<TMessage> :
    IBatchConfigurator<TMessage>
    where TMessage : class
{
    readonly IReceiveEndpointConfigurator _configurator;

    /// <summary>Initializes a new instance.</summary>
    /// <param name="configurator">The configurator to update.</param>
    public BatchConfigurator(IReceiveEndpointConfigurator configurator)
    {
        _configurator = configurator;

        ConcurrencyLimit = 1;
        MessageLimit = 10;
        TimeLimit = TimeSpan.FromSeconds(10);
        TimeLimitStart = BatchTimeLimitStart.FromFirst;
    }

    /// <summary>Gets or sets the time limit.</summary>
    public TimeSpan TimeLimit { private get; set; }
    /// <summary>Gets or sets the time limit start.</summary>
    public BatchTimeLimitStart TimeLimitStart { private get; set; }
    /// <summary>Gets or sets the message limit.</summary>
    public int MessageLimit { private get; set; }
    /// <summary>Gets or sets the concurrency limit.</summary>
    public int ConcurrencyLimit { private get; set; }

    /// <summary>Configures batch consumption for the selected consumer type.</summary>
    /// <typeparam name="TConsumer">The consumer implementation used by the member.</typeparam>
    /// <param name="consumerFactory">The consumer factory.</param>
    /// <param name="configure">The callback used to configure the component.</param>
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
