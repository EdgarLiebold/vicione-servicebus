using System;
using System.Linq;
using ViciOne.ServiceBus.RabbitMq.Configuration;

namespace ViciOne.ServiceBus.RabbitMq;

/// <summary>Configures RabbitMQ-native delayed redelivery through predeclared TTL queues.</summary>
public static class RabbitMqQueueRedeliveryExtensions
{
    /// <summary>
    /// Applies the canonical ViciOne technical redelivery schedule and failure taxonomy using
    /// RabbitMQ-native predeclared TTL/DLX queues. This is the preferred RabbitMQ companion to
    /// <see cref="TechnicalRetryConfigurationExtensions.UseTechnicalMessageRetry" />.
    /// </summary>
    /// <param name="configurator">The RabbitMQ receive-endpoint configurator.</param>
    /// <param name="classifier">The failure classifier, or the canonical technical classifier when omitted.</param>
    public static void UseTechnicalQueueRedelivery(this IRabbitMqReceiveEndpointConfigurator configurator,
        ITechnicalFailureClassifier? classifier = null)
    {
        ArgumentNullException.ThrowIfNull(configurator);

        classifier ??= TechnicalRetryPolicy.DefaultFailureClassifier;
        TimeSpan[] intervals = TechnicalRetryPolicy.RedeliveryIntervals.ToArray();
        configurator.UseQueueRedelivery(intervals, retry =>
            retry.Handle<Exception>(exception => classifier.Classify(exception) == RetryFailureKind.Transient));
    }

    /// <summary>
    /// Configures RabbitMQ-native technical redelivery through a finite set of predeclared TTL/DLX
    /// queues. This path does not require the delayed-message exchange plugin.
    /// </summary>
    /// <param name="configurator">The RabbitMQ receive-endpoint configurator.</param>
    /// <param name="intervals">The finite set of delay intervals to predeclare.</param>
    public static void UseQueueRedelivery(this IRabbitMqReceiveEndpointConfigurator configurator, params TimeSpan[] intervals)
    {
        ArgumentNullException.ThrowIfNull(configurator);
        ArgumentNullException.ThrowIfNull(intervals);

        if (configurator is not RabbitMqReceiveEndpointConfiguration endpointConfiguration)
            throw new ConfigurationException(global::ViciOne.ServiceBus.Providers.Configuration.ConfigurationMessages.Create("RabbitMQ", "unknown", "RabbitMQ queue redelivery requires the native RabbitMQ receive-endpoint configuration.", "Correct the named configuration before starting the host"));

        var snapshot = intervals.ToArray();
        var plan = endpointConfiguration.CreateQueueRedeliveryPlan(snapshot);
        _ = new RabbitMqQueueRedeliveryConfigurationObserver(endpointConfiguration, plan, retry => retry.Intervals(snapshot));
    }

    /// <summary>
    /// Configures finite RabbitMQ-native technical redelivery and allows exception selection to be
    /// refined. Runtime delay values outside the declared set are rejected.
    /// </summary>
    /// <param name="configurator">The RabbitMQ receive-endpoint configurator.</param>
    /// <param name="intervals">The finite set of delay intervals to predeclare.</param>
    /// <param name="configure">The callback that selects which exceptions are redelivered.</param>
    public static void UseQueueRedelivery(this IRabbitMqReceiveEndpointConfigurator configurator, TimeSpan[] intervals,
        Action<IRedeliveryConfigurator> configure)
    {
        ArgumentNullException.ThrowIfNull(configurator);
        ArgumentNullException.ThrowIfNull(intervals);
        ArgumentNullException.ThrowIfNull(configure);

        if (configurator is not RabbitMqReceiveEndpointConfiguration endpointConfiguration)
            throw new ConfigurationException(global::ViciOne.ServiceBus.Providers.Configuration.ConfigurationMessages.Create("RabbitMQ", "unknown", "RabbitMQ queue redelivery requires the native RabbitMQ receive-endpoint configuration.", "Correct the named configuration before starting the host"));

        var snapshot = intervals.ToArray();
        var plan = endpointConfiguration.CreateQueueRedeliveryPlan(snapshot);
        _ = new RabbitMqQueueRedeliveryConfigurationObserver(endpointConfiguration, plan, retry =>
        {
            configure(retry);
            retry.Intervals(snapshot);
        });
    }
}
