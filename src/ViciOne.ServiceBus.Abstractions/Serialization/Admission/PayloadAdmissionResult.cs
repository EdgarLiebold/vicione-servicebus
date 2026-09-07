namespace ViciOne.ServiceBus.Advanced.Serialization;

/// <summary>The immutable decision for one exact serialized application body.</summary>
/// <param name="Disposition">The selected body-storage strategy.</param>
/// <param name="SerializedBodyBytes">The exact serialized application-body size.</param>
/// <param name="WarningThresholdExceeded">Whether the configured warning threshold was exceeded.</param>
public readonly record struct PayloadAdmissionResult(
    PayloadAdmissionDisposition Disposition,
    int SerializedBodyBytes,
    bool WarningThresholdExceeded);
