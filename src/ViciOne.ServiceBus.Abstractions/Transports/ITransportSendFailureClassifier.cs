namespace ViciOne.ServiceBus;

using System;


public enum TransportSendFailureKind
{
    Transient = 0,
    Permanent = 1,
    Unclassified = 2
}

/// <summary>
/// Allows a transport package to classify send failures without leaking provider-specific exception types into
/// persistence/reliability components. Returning false delegates classification to the next classifier.
/// </summary>
public interface ITransportSendFailureClassifier
{
    bool TryClassify(Exception exception, out TransportSendFailureKind failureKind);
}
