using System;
using ViciOne.ServiceBus.Configuration;

namespace ViciOne.ServiceBus.RabbitMq.Configuration;

/// <summary>Adds RabbitMQ queue-redelivery and retry specifications for each configured message contract.</summary>
public sealed class RabbitMqQueueRedeliveryConfigurationObserver : ConfigurationObserver, IMessageConfigurationObserver
{
    readonly Action<IRedeliveryConfigurator> _configure;
    readonly IConsumePipeConfigurator _configurator;
    readonly RabbitMqQueueRedeliveryPlan _plan;

    /// <summary>Connects the observer after capturing the endpoint, redelivery plan, and retry callback.</summary>
    /// <param name="configurator">The consume pipeline configurator.</param>
    /// <param name="plan">The finite RabbitMQ redelivery-queue plan.</param>
    /// <param name="configure">The callback that selects retryable exceptions.</param>
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

    /// <summary>Adds redelivery and retry filters for a configured message contract.</summary>
    /// <typeparam name="TMessage">The message contract.</typeparam>
    /// <param name="configurator">The message's consume pipeline configurator.</param>
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
