using System;
using ViciOne.ServiceBus.Configuration;

namespace ViciOne.ServiceBus.RabbitMqTransport.Configuration;

public sealed class RabbitMqQueueRedeliveryConfigurationObserver : ConfigurationObserver, IMessageConfigurationObserver
{
    readonly Action<IRedeliveryConfigurator> _configure;
    readonly IConsumePipeConfigurator _configurator;
    readonly RabbitMqQueueRedeliveryPlan _plan;

    public RabbitMqQueueRedeliveryConfigurationObserver(IConsumePipeConfigurator configurator, RabbitMqQueueRedeliveryPlan plan,
        Action<IRedeliveryConfigurator> configure)
        : base(configurator)
    {
        _configurator = configurator;
        _plan = plan;
        _configure = configure;

        // ConfigurationObserver may replay existing message types immediately. Connect only after
        // the derived instance is fully initialized.
        Connect(this);
    }

    public void MessageConfigured<TMessage>(IConsumePipeConfigurator configurator)
        where TMessage : class
    {
        var redeliverySpecification = new RabbitMqQueueRedeliveryPipeSpecification<TMessage>(_plan);
        _configurator.AddPipeSpecification(redeliverySpecification);

        var retrySpecification = new RedeliveryRetryPipeSpecification<TMessage>(redeliverySpecification);
        _configure(retrySpecification);
        _configurator.AddPipeSpecification(retrySpecification);
    }
}
