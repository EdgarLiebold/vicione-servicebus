using System;

namespace ViciOne.ServiceBus.Serialization;
/// <summary>Raised when a serialized payload crosses a configured admission boundary.</summary>
public sealed class PayloadAdmissionException : Exception
{
    /// <summary>Creates one payload-admission failure.</summary>
    public PayloadAdmissionException(
        PayloadAdmissionStage stage,
        long actualBytes,
        long? configuredLimitBytes,
        string message)
        : base(message)
    {
        if (!Enum.IsDefined(stage))
            throw new ArgumentOutOfRangeException(nameof(stage), stage, "Unknown payload admission stage.");
        if (actualBytes < 0)
            throw new ArgumentOutOfRangeException(nameof(actualBytes));
        if (configuredLimitBytes is <= 0)
            throw new ArgumentOutOfRangeException(nameof(configuredLimitBytes));
        ArgumentException.ThrowIfNullOrWhiteSpace(message);

        Stage = stage;
        ActualBytes = actualBytes;
        ConfiguredLimitBytes = configuredLimitBytes;
    }

    /// <summary>Gets the boundary that rejected the payload.</summary>
    public PayloadAdmissionStage Stage { get; }

    /// <summary>
    /// Gets the exact encoded length for materialized bytes, or the minimum requested/required capacity
    /// at a bounded-writer rejection.
    /// </summary>
    public long ActualBytes { get; }

    /// <summary>Gets the configured boundary that was crossed, when one exists.</summary>
    public long? ConfiguredLimitBytes { get; }
}
