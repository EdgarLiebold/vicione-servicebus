using System;

namespace ViciOne.ServiceBus.Advanced;

/// <summary>Classifies exceptions for infrastructure retry and redelivery without relying on exception text.</summary>
public interface ITechnicalFailureClassifier
{
    /// <summary>Classifies <paramref name="exception" /> for technical retry.</summary>
    /// <param name="exception">The failure to classify.</param>
    /// <returns>The retry behavior assigned to the failure.</returns>
    RetryFailureKind Classify(Exception exception);
}
