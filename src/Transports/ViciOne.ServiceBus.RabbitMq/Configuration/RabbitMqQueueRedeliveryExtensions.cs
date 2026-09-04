using System;
using System.Linq;
using ViciOne.ServiceBus.RabbitMq.Configuration;

#nullable enable
namespace ViciOne.ServiceBus.RabbitMq;

/// <summary>
/// Provides extension methods for rabbit mq queue redelivery.
/// </summary>
public static class RabbitMqQueueRedeliveryExtensions
{
    /// <summary>
    /// Applies the canonical ViciOne technical redelivery schedule and failure taxonomy using
    /// RabbitMQ-native predeclared TTL/DLX queues. This is the preferred RabbitMQ companion to
    /// <see cref="TechnicalRetryConfigurationExtensions.UseTechnicalMessageRetry" />.
    /// </summary>
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
