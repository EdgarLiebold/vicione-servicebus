using System;

namespace ViciOne.ServiceBus.Providers.Transports;

/// <summary>
/// Specifies the available transport send failure kind values.
/// </summary>
public enum TransportSendFailureKind
{
    /// <summary>
    /// Indicates transient.
    /// </summary>
    Transient = 0,
    /// <summary>
    /// Indicates permanent.
    /// </summary>
    Permanent = 1,
    /// <summary>
    /// Indicates unclassified.
    /// </summary>
    Unclassified = 2
}

/// <summary>
/// Allows a transport package to classify send failures without leaking provider-specific exception types into
/// persistence/reliability components. Returning false delegates classification to the next classifier.
/// </summary>
public interface ITransportSendFailureClassifier
{
    /// <summary>
    /// Performs the try classify operation.
    /// </summary>
    /// <param name="exception">The exception associated with the operation.</param>
    /// <param name="failureKind">The failure kind value.</param>
    /// <returns><see langword="true" /> when the condition is satisfied; otherwise, <see langword="false" />.</returns>
    bool TryClassify(Exception exception, out TransportSendFailureKind failureKind);
}
