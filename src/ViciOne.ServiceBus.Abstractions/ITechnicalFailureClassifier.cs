using System;

namespace ViciOne.ServiceBus.Advanced;
/// <summary>Classifies exceptions for infrastructure retry and redelivery without relying on exception text.</summary>
public interface ITechnicalFailureClassifier
{
    /// <summary>Classifies <paramref name="exception" /> for technical retry.</summary>
    /// <param name="exception">The exception associated with the operation.</param>
    /// <returns>The retry failure kind produced by the operation.</returns>
    RetryFailureKind Classify(Exception exception);
}

/// <summary>Allows an application or provider exception to state its own technical retry meaning.</summary>
public interface IRetryFailureClassification
{
    /// <summary>Gets the retry failure kind.</summary>
    RetryFailureKind RetryFailureKind { get; }
}
