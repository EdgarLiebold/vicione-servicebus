namespace ViciOne.ServiceBus.Advanced.Serialization;

/// <summary>The immutable decision for one exact serialized application body.</summary>
public readonly record struct PayloadAdmissionResult(
    PayloadAdmissionDisposition Disposition,
    int SerializedBodyBytes,
    bool WarningThresholdExceeded);
