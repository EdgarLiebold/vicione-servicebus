namespace ViciOne.ServiceBus;

using System;
using System.Linq;
using RabbitMqTransport.Configuration;


public static class RabbitMqQueueRedeliveryExtensions
{
    /// <summary>
    /// Configures RabbitMQ-native technical redelivery through a finite set of predeclared TTL/DLX
    /// queues. This path does not require the delayed-message exchange plugin.
    /// </summary>
    public static void UseQueueRedelivery(this IRabbitMqReceiveEndpointConfigurator configurator, params TimeSpan[] intervals)
    {
        ArgumentNullException.ThrowIfNull(configurator);
        ArgumentNullException.ThrowIfNull(intervals);

        if (configurator is not RabbitMqReceiveEndpointConfiguration endpointConfiguration)
            throw new ConfigurationException("RabbitMQ queue redelivery requires the native RabbitMQ receive-endpoint configuration.");

        var snapshot = intervals.ToArray();
        var plan = endpointConfiguration.CreateQueueRedeliveryPlan(snapshot);
        _ = new RabbitMqQueueRedeliveryConfigurationObserver(endpointConfiguration, plan, retry => retry.Intervals(snapshot));
    }

    /// <summary>
    /// Configures finite RabbitMQ-native technical redelivery and allows exception selection to be
    /// refined. Runtime delay values outside the declared set are rejected.
    /// </summary>
    public static void UseQueueRedelivery(this IRabbitMqReceiveEndpointConfigurator configurator, TimeSpan[] intervals,
        Action<IRedeliveryConfigurator> configure)
    {
        ArgumentNullException.ThrowIfNull(configurator);
        ArgumentNullException.ThrowIfNull(intervals);
        ArgumentNullException.ThrowIfNull(configure);

        if (configurator is not RabbitMqReceiveEndpointConfiguration endpointConfiguration)
            throw new ConfigurationException("RabbitMQ queue redelivery requires the native RabbitMQ receive-endpoint configuration.");

        var snapshot = intervals.ToArray();
        var plan = endpointConfiguration.CreateQueueRedeliveryPlan(snapshot);
        _ = new RabbitMqQueueRedeliveryConfigurationObserver(endpointConfiguration, plan, retry =>
        {
            configure(retry);
            retry.Intervals(snapshot);
        });
    }
}
