using System;

namespace ViciOne.ServiceBus.Configuration;

/// <summary>Observes delayed redelivery configuration events.</summary>
public class DelayedRedeliveryConfigurationObserver :
    ScheduledRedeliveryConfigurationObserver
{
    /// <summary>Initializes a new instance.</summary>
    /// <param name="configurator">The configurator to update.</param>
    /// <param name="configure">The callback used to configure the component.</param>
    public DelayedRedeliveryConfigurationObserver(IConsumePipeConfigurator configurator, Action<IRedeliveryConfigurator> configure)
        : base(configurator, configure)
    {
    }

    /// <summary>Adds redelivery pipe specification to the configuration.</summary>
    /// <typeparam name="TMessage">The message contract processed by the member.</typeparam>
    /// <param name="configurator">The configurator to update.</param>
    /// <returns>The redelivery pipe specification produced by the operation.</returns>
    protected override IRedeliveryPipeSpecification AddRedeliveryPipeSpecification<TMessage>(IConsumePipeConfigurator configurator)
    {
        var redeliverySpecification = new DelayedRedeliveryPipeSpecification<TMessage>();

        configurator.AddPipeSpecification(redeliverySpecification);

        return redeliverySpecification;
    }
}
