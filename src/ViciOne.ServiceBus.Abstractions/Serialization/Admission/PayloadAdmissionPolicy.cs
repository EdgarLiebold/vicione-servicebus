using System;

namespace ViciOne.ServiceBus.Advanced.Serialization;

/// <summary>Defines immutable size thresholds for serialized message bodies and transport envelopes.</summary>
public sealed record PayloadAdmissionPolicy
{
    /// <summary>Gets the optional serialized-body size above which a warning is recorded.</summary>
    public int? WarningBodyBytes { get; init; }

    /// <summary>Gets the optional serialized-body size above which MessageData offload is required.</summary>
    public int? MessageDataOffloadThresholdBytes { get; init; }

    /// <summary>Gets the inclusive maximum serialized application-body size.</summary>
    public required int MaximumSerializedBodyBytes { get; init; }

    /// <summary>Gets the inclusive maximum size of the final serialized transport envelope.</summary>
    public required int MaximumTransportEnvelopeBytes { get; init; }

    /// <summary>Validates the configured thresholds.</summary>
    /// <returns>This policy after successful validation.</returns>
    /// <exception cref="ArgumentException">A threshold is not positive or exceeds its applicable hard maximum.</exception>
    public PayloadAdmissionPolicy Validate()
    {
        ValidatePositiveOrNull(WarningBodyBytes, nameof(WarningBodyBytes));
        ValidatePositiveOrNull(MessageDataOffloadThresholdBytes, nameof(MessageDataOffloadThresholdBytes));
        ValidatePositive(MaximumSerializedBodyBytes, nameof(MaximumSerializedBodyBytes));
        ValidatePositive(MaximumTransportEnvelopeBytes, nameof(MaximumTransportEnvelopeBytes));

        if (MessageDataOffloadThresholdBytes is { } offload
            && offload > MaximumSerializedBodyBytes)
        {
            throw new ArgumentException(
                "MessageData offload threshold cannot exceed the maximum serialized body size.",
                nameof(MessageDataOffloadThresholdBytes));
        }

        if (WarningBodyBytes is { } warning
            && warning > MaximumSerializedBodyBytes)
        {
            throw new ArgumentException(
                "Payload warning threshold cannot exceed the maximum serialized body size.",
                nameof(WarningBodyBytes));
        }

        if (MaximumTransportEnvelopeBytes < MaximumSerializedBodyBytes)
        {
            throw new ArgumentException(
                "Maximum transport envelope size cannot be less than the maximum serialized body size.",
                nameof(MaximumTransportEnvelopeBytes));
        }

        return this;
    }

    private static void ValidatePositiveOrNull(int? value, string parameterName)
    {
        if (value is <= 0)
            throw new ArgumentOutOfRangeException(parameterName, value, "Payload size thresholds must be positive when specified.");
    }

    private static void ValidatePositive(int value, string parameterName)
    {
        if (value <= 0)
            throw new ArgumentOutOfRangeException(parameterName, value, "Payload size limits must be positive.");
    }
}
