using System;

namespace ViciOne.ServiceBus.Configuration;

public class DelayedRedeliveryConfigurationObserver :
    ScheduledRedeliveryConfigurationObserver
{
    public DelayedRedeliveryConfigurationObserver(IConsumePipeConfigurator configurator, Action<IRedeliveryConfigurator> configure)
        : base(configurator, configure)
    {
    }

    protected override IRedeliveryPipeSpecification AddRedeliveryPipeSpecification<TMessage>(IConsumePipeConfigurator configurator)
    {
        var redeliverySpecification = new DelayedRedeliveryPipeSpecification<TMessage>();

        configurator.AddPipeSpecification(redeliverySpecification);

        return redeliverySpecification;
    }
}
