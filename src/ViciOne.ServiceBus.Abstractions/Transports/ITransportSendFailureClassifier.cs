using System;

namespace ViciOne.ServiceBus.Providers.Transports;

/// <summary>Specifies the available transport send failure kind values.</summary>
public enum TransportSendFailureKind
{
    /// <summary>Indicates transient.</summary>
    Transient = 0,
    /// <summary>Indicates permanent.</summary>
    Permanent = 1,
    /// <summary>Indicates unclassified.</summary>
    Unclassified = 2
}

/// <summary>
/// Allows a transport package to classify send failures without leaking provider-specific exception types into
/// persistence/reliability components. Returning false delegates classification to the next classifier.
/// </summary>
public interface ITransportSendFailureClassifier
{
    /// <summary>Attempts to classify.</summary>
    /// <param name="exception">The exception associated with the operation.</param>
    /// <param name="failureKind">Receives the failure kind produced by the operation.</param>
    /// <returns><see langword="true" /> when the condition is satisfied; otherwise, <see langword="false" />.</returns>
    bool TryClassify(Exception exception, out TransportSendFailureKind failureKind);
}
