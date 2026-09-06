using System;
using System.Collections.Generic;
using ViciOne.ServiceBus.RetryPolicies;

namespace ViciOne.ServiceBus.Configuration;
/// <summary>
/// Provides the canonical bounded policy for technical message retry and redelivery.
/// </summary>
public static class TechnicalRetryPolicy
{
    static readonly TimeSpan[] _immediateIntervals =
    [
        TimeSpan.FromMilliseconds(100),
        TimeSpan.FromMilliseconds(500),
        TimeSpan.FromSeconds(2),
    ];

    static readonly TimeSpan[] _redeliveryIntervals =
    [
        TimeSpan.FromSeconds(15),
        TimeSpan.FromMinutes(1),
        TimeSpan.FromMinutes(5),
    ];

    static readonly IReadOnlyList<TimeSpan> _immediateIntervalsView = Array.AsReadOnly(_immediateIntervals);
    static readonly IReadOnlyList<TimeSpan> _redeliveryIntervalsView = Array.AsReadOnly(_redeliveryIntervals);

    /// <summary>
    /// Gets the default conservative technical failure classifier.
    /// </summary>
    public static ITechnicalFailureClassifier DefaultFailureClassifier { get; } = new DefaultTechnicalFailureClassifier();

    /// <summary>
    /// Gets the canonical bounded in-process retry sequence: 100 milliseconds, 500 milliseconds and two
    /// seconds.
    /// </summary>
    public static IReadOnlyList<TimeSpan> ImmediateIntervals => _immediateIntervalsView;

    /// <summary>
    /// Gets the canonical durable redelivery sequence: 15 seconds, one minute and five minutes.
    /// </summary>
    public static IReadOnlyList<TimeSpan> RedeliveryIntervals => _redeliveryIntervalsView;

    /// <summary>
    /// Configures the canonical in-process policy, restricted to explicitly transient failures.
    /// </summary>
    /// <param name="configurator">The retry configurator.</param>
    /// <param name="classifier">An optional classifier; the default classifier is used when omitted.</param>
    public static void ConfigureImmediate(IRetryConfigurator configurator, ITechnicalFailureClassifier? classifier = null)
    {
        ArgumentNullException.ThrowIfNull(configurator);

        classifier ??= DefaultFailureClassifier;
        configurator.Handle<Exception>(exception => classifier.Classify(exception) == RetryFailureKind.Transient);
        configurator.Intervals((TimeSpan[])_immediateIntervals.Clone());
    }

    /// <summary>
    /// Configures the canonical delayed policy, restricted to explicitly transient failures.
    /// </summary>
    /// <param name="configurator">The redelivery configurator.</param>
    /// <param name="classifier">An optional classifier; the default classifier is used when omitted.</param>
    public static void ConfigureRedelivery(IRedeliveryConfigurator configurator, ITechnicalFailureClassifier? classifier = null)
    {
        ArgumentNullException.ThrowIfNull(configurator);

        classifier ??= DefaultFailureClassifier;
        configurator.Handle<Exception>(exception => classifier.Classify(exception) == RetryFailureKind.Transient);
        configurator.Intervals((TimeSpan[])_redeliveryIntervals.Clone());
    }
}
