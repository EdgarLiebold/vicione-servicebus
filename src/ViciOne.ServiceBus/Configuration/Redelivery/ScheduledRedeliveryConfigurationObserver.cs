using System;

namespace ViciOne.ServiceBus.Configuration;

/// <summary>Adds scheduled-redelivery specifications to configured message and activity pipelines.</summary>
public class ScheduledRedeliveryConfigurationObserver :
    ConfigurationObserver,
    IMessageConfigurationObserver
{
    readonly IConsumePipeConfigurator _configurator;
    readonly Action<IRedeliveryConfigurator> _configure;

    /// <summary>Creates an observer for a consume pipeline and a shared redelivery policy callback.</summary>
    /// <param name="configurator">The consume pipeline whose configurations are observed.</param>
    /// <param name="configure">The callback applied to each redelivery policy.</param>
    public ScheduledRedeliveryConfigurationObserver(IConsumePipeConfigurator configurator, Action<IRedeliveryConfigurator> configure)
        : base(configurator)
    {
        _configurator = configurator;
        _configure = configure;

        Connect(this);
    }

    /// <summary>Adds scheduled redelivery to a configured message pipeline.</summary>
    /// <typeparam name="TMessage">The configured message contract.</typeparam>
    /// <param name="configurator">The consume pipeline that owns the message pipeline.</param>
    public void MessageConfigured<TMessage>(IConsumePipeConfigurator configurator)
        where TMessage : class
    {
        var redeliveryPipeSpecification = AddRedeliveryPipeSpecification<TMessage>(configurator);

        if (typeof(TMessage).IsDefined(typeof(ActivityMessageAttribute), inherit: false))
            return;

        var retrySpecification = new RedeliveryRetryPipeSpecification<TMessage>(redeliveryPipeSpecification);

        _configure?.Invoke(retrySpecification);

        configurator.AddPipeSpecification(retrySpecification);
    }

    /// <summary>Adds scheduled redelivery for each message collected by a configured batch consumer.</summary>
    /// <typeparam name="TConsumer">The batch consumer implementation.</typeparam>
    /// <typeparam name="TMessage">The message contract contained by the batch.</typeparam>
    /// <param name="configurator">The configured batch-consumer pipeline.</param>
    public override void BatchConsumerConfigured<TConsumer, TMessage>(IConsumerMessageConfigurator<TConsumer, IMessageBatch<TMessage>> configurator)
    {
        MessageConfigured<TMessage>(_configurator);
    }

    /// <summary>Adds scheduled redelivery to a configured activity execution pipeline.</summary>
    /// <typeparam name="TActivity">The activity type.</typeparam>
    /// <typeparam name="TArguments">The arguments type.</typeparam>
    /// <param name="configurator">The configurator to update.</param>
    /// <param name="compensateAddress">The compensate address.</param>
    public override void ActivityConfigured<TActivity, TArguments>(IExecuteActivityPipeConfigurator<TActivity, TArguments> configurator, Uri compensateAddress)
    {
        base.ActivityConfigured(configurator, compensateAddress);

        var specification = new ExecuteContextRedeliveryPipeSpecification<TArguments>();

        _configure?.Invoke(specification);

        configurator.Arguments(x => x.AddPipeSpecification(specification));
    }

    /// <summary>Adds scheduled redelivery to a configured execute-activity pipeline.</summary>
    /// <typeparam name="TActivity">The activity type.</typeparam>
    /// <typeparam name="TArguments">The arguments type.</typeparam>
    /// <param name="configurator">The configurator to update.</param>
    public override void ExecuteActivityConfigured<TActivity, TArguments>(IExecuteActivityPipeConfigurator<TActivity, TArguments> configurator)
    {
        base.ExecuteActivityConfigured(configurator);

        var specification = new ExecuteContextRedeliveryPipeSpecification<TArguments>();

        _configure?.Invoke(specification);

        configurator.Arguments(x => x.AddPipeSpecification(specification));
    }

    /// <summary>Adds scheduled redelivery to a configured compensate-activity pipeline.</summary>
    /// <typeparam name="TActivity">The activity type.</typeparam>
    /// <typeparam name="TLog">The log type.</typeparam>
    /// <param name="configurator">The configurator to update.</param>
    public override void CompensateActivityConfigured<TActivity, TLog>(ICompensateActivityPipeConfigurator<TActivity, TLog> configurator)
    {
        base.CompensateActivityConfigured(configurator);

        var specification = new CompensateContextRedeliveryPipeSpecification<TLog>();

        _configure?.Invoke(specification);

        configurator.Log(x => x.AddPipeSpecification(specification));
    }

    /// <summary>Adds a scheduled-redelivery specification to a message pipeline.</summary>
    /// <typeparam name="TMessage">The message contract processed by the member.</typeparam>
    /// <param name="configurator">The configurator to update.</param>
    /// <returns>The redelivery pipe specification produced by the operation.</returns>
    protected virtual IRedeliveryPipeSpecification AddRedeliveryPipeSpecification<TMessage>(IConsumePipeConfigurator configurator)
        where TMessage : class
    {
        var redeliverySpecification = new ScheduledRedeliveryPipeSpecification<TMessage>();

        configurator.AddPipeSpecification(redeliverySpecification);

        return redeliverySpecification;
    }
}
