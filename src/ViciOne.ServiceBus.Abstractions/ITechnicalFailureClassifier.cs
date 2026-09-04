using System;

namespace ViciOne.ServiceBus;
/// <summary>
/// Classifies exceptions for infrastructure retry and redelivery without relying on exception text.
/// </summary>
public interface ITechnicalFailureClassifier
{
    /// <summary>
    /// Classifies <paramref name="exception" /> for technical retry.
    /// </summary>
    /// <param name="exception">The failure to classify.</param>
    /// <returns>The owned retry classification.</returns>
    RetryFailureKind Classify(Exception exception);
}

/// <summary>
/// Allows an application or provider exception to state its own technical retry meaning.
/// </summary>
public interface IRetryFailureClassification
{
    /// <summary>
    /// Gets the exception-owned retry classification.
    /// </summary>
    RetryFailureKind RetryFailureKind { get; }
}
