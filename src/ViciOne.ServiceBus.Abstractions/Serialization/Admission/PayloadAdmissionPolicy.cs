using System;

namespace ViciOne.ServiceBus.Advanced.Serialization;
/// <summary>Bus-owned immutable policy for the application body and final transport envelope.</summary>
public sealed record PayloadAdmissionPolicy
{
    /// <summary>Gets or sets the warning body bytes.</summary>
    public int? WarningBodyBytes { get; init; }

    /// <summary>Gets or sets the message data offload threshold bytes.</summary>
    public int? MessageDataOffloadThresholdBytes { get; init; }

    /// <summary>Gets or sets the maximum serialized body bytes.</summary>
    public int? MaximumSerializedBodyBytes { get; init; }

    /// <summary>Gets or sets the maximum transport envelope bytes.</summary>
    public int? MaximumTransportEnvelopeBytes { get; init; }

    /// <summary>Validates and returns this immutable policy.</summary>
    /// <returns>The validation failures.</returns>
    public PayloadAdmissionPolicy Validate()
    {
        ValidatePositiveOrNull(WarningBodyBytes, nameof(WarningBodyBytes));
        ValidatePositiveOrNull(MessageDataOffloadThresholdBytes, nameof(MessageDataOffloadThresholdBytes));
        ValidatePositiveOrNull(MaximumSerializedBodyBytes, nameof(MaximumSerializedBodyBytes));
        ValidatePositiveOrNull(MaximumTransportEnvelopeBytes, nameof(MaximumTransportEnvelopeBytes));

        if (MessageDataOffloadThresholdBytes is { } offload
            && MaximumSerializedBodyBytes is { } maximum
            && offload > maximum)
        {
            throw new ArgumentException(
                "MessageData offload threshold cannot exceed the maximum serialized body size.",
                nameof(MessageDataOffloadThresholdBytes));
        }

        if (WarningBodyBytes is { } warning
            && MaximumSerializedBodyBytes is { } hardMaximum
            && warning > hardMaximum)
        {
            throw new ArgumentException(
                "Payload warning threshold cannot exceed the maximum serialized body size.",
                nameof(WarningBodyBytes));
        }

        return this;
    }

    private static void ValidatePositiveOrNull(int? value, string parameterName)
    {
        if (value is <= 0)
            throw new ArgumentOutOfRangeException(parameterName, value, "Payload size thresholds must be positive when specified.");
    }
}
