namespace ViciOne.ServiceBus.Advanced.Serialization;

/// <summary>The immutable decision for one exact serialized application body.</summary>
/// <param name="Disposition">The disposition.</param>
/// <param name="SerializedBodyBytes">The serialized body bytes.</param>
/// <param name="WarningThresholdExceeded">The warning threshold exceeded.</param>
public readonly record struct PayloadAdmissionResult(
    PayloadAdmissionDisposition Disposition,
    int SerializedBodyBytes,
    bool WarningThresholdExceeded);
