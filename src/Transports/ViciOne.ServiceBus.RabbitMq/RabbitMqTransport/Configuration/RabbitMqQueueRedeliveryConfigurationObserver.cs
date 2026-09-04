using System;
using ViciOne.ServiceBus.Configuration;

namespace ViciOne.ServiceBus.RabbitMq.Configuration;

/// <summary>
/// Provides a rabbit mq queue redelivery configuration observer implementation.
/// </summary>
public sealed class RabbitMqQueueRedeliveryConfigurationObserver : ConfigurationObserver, IMessageConfigurationObserver
{
    readonly Action<IRedeliveryConfigurator> _configure;
    readonly IConsumePipeConfigurator _configurator;
    readonly RabbitMqQueueRedeliveryPlan _plan;

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="configurator">The configurator value.</param>
    /// <param name="plan">The plan value.</param>
    /// <param name="configure">The configuration callback.</param>
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

    /// <summary>
    /// Performs the message configured operation.
    /// </summary>
    /// <typeparam name="TMessage">The t message type.</typeparam>
    /// <param name="configurator">The configurator value.</param>
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
