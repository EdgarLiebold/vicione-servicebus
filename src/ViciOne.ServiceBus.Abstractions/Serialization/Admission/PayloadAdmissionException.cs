using System;

namespace ViciOne.ServiceBus;
/// <summary>Raised when a serialized payload crosses a configured admission boundary.</summary>
public sealed class PayloadAdmissionException : Exception
{
    /// <summary>Creates one payload-admission failure.</summary>
    /// <param name="stage">The stage.</param>
    /// <param name="actualBytes">The actual bytes.</param>
    /// <param name="configuredLimitBytes">The configured limit bytes.</param>
    /// <param name="message">The message to process.</param>
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

    /// <summary>Gets the stage.</summary>
    public PayloadAdmissionStage Stage { get; }

    /// <summary>Gets the actual bytes.</summary>
    public long ActualBytes { get; }

    /// <summary>Gets the configured limit bytes.</summary>
    public long? ConfiguredLimitBytes { get; }
}
