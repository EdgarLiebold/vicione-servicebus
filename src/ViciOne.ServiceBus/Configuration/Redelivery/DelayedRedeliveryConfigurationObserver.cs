using System;

namespace ViciOne.ServiceBus.Configuration;

/// <summary>
/// Provides a delayed redelivery configuration observer implementation.
/// </summary>
public class DelayedRedeliveryConfigurationObserver :
    ScheduledRedeliveryConfigurationObserver
{
    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="configurator">The configurator value.</param>
    /// <param name="configure">The configuration callback.</param>
    public DelayedRedeliveryConfigurationObserver(IConsumePipeConfigurator configurator, Action<IRedeliveryConfigurator> configure)
        : base(configurator, configure)
    {
    }

    /// <summary>
    /// Adds redelivery pipe specification to the configuration.
    /// </summary>
    /// <typeparam name="TMessage">The t message type.</typeparam>
    /// <param name="configurator">The configurator value.</param>
    /// <returns>The result of the operation.</returns>
    protected override IRedeliveryPipeSpecification AddRedeliveryPipeSpecification<TMessage>(IConsumePipeConfigurator configurator)
    {
        var redeliverySpecification = new DelayedRedeliveryPipeSpecification<TMessage>();

        configurator.AddPipeSpecification(redeliverySpecification);

        return redeliverySpecification;
    }
}
