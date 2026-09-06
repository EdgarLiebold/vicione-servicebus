using System;

namespace ViciOne.ServiceBus.Configuration;

/// <summary>Observes scheduled redelivery configuration events.</summary>
public class ScheduledRedeliveryConfigurationObserver :
    ConfigurationObserver,
    IMessageConfigurationObserver
{
    readonly IConsumePipeConfigurator _configurator;
    readonly Action<IRedeliveryConfigurator> _configure;

    /// <summary>Initializes a new instance.</summary>
    /// <param name="configurator">The configurator to update.</param>
    /// <param name="configure">The callback used to configure the component.</param>
    public ScheduledRedeliveryConfigurationObserver(IConsumePipeConfigurator configurator, Action<IRedeliveryConfigurator> configure)
        : base(configurator)
    {
        _configurator = configurator;
        _configure = configure;

        Connect(this);
    }

    /// <summary>Reports that message has been configured.</summary>
    /// <typeparam name="TMessage">The message contract processed by the member.</typeparam>
    /// <param name="configurator">The configurator to update.</param>
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

    /// <summary>Reports that batch consumer has been configured.</summary>
    /// <typeparam name="TConsumer">The consumer implementation used by the member.</typeparam>
    /// <typeparam name="TMessage">The message contract processed by the member.</typeparam>
    /// <param name="configurator">The configurator to update.</param>
    public override void BatchConsumerConfigured<TConsumer, TMessage>(IConsumerMessageConfigurator<TConsumer, Batch<TMessage>> configurator)
    {
        MessageConfigured<TMessage>(_configurator);
    }

    /// <summary>Reports that activity has been configured.</summary>
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

    /// <summary>Reports that execute activity has been configured.</summary>
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

    /// <summary>Reports that compensate activity has been configured.</summary>
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

    /// <summary>Adds redelivery pipe specification to the configuration.</summary>
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
