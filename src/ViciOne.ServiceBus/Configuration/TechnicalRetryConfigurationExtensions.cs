using System;

namespace ViciOne.ServiceBus.Configuration;

/// <summary>Configures canonical bounded technical retry and redelivery on consume pipelines.</summary>
public static class TechnicalRetryConfigurationExtensions
{
    /// <summary>Adds the canonical short in-process technical retry policy.</summary>
    /// <param name="configurator">The configurator to update.</param>
    /// <param name="classifier">The classifier.</param>
    public static void UseTechnicalMessageRetry(this IConsumePipeConfigurator configurator,
        ITechnicalFailureClassifier? classifier = null)
    {
        ArgumentNullException.ThrowIfNull(configurator);
        configurator.UseMessageRetry(retry => TechnicalRetryPolicy.ConfigureImmediate(retry, classifier));
    }

    /// <summary>Adds the canonical delayed technical redelivery policy.</summary>
    /// <param name="configurator">The configurator to update.</param>
    /// <param name="classifier">The classifier.</param>
    public static void UseTechnicalDelayedRedelivery(this IConsumePipeConfigurator configurator,
        ITechnicalFailureClassifier? classifier = null)
    {
        ArgumentNullException.ThrowIfNull(configurator);
        configurator.UseDelayedRedelivery(redelivery => TechnicalRetryPolicy.ConfigureRedelivery(redelivery, classifier));
    }
}
