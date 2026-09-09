namespace ViciOne.ServiceBus;

/// <summary>Indicates that a serialized payload exceeded a configured admission limit.</summary>
public sealed class PayloadAdmissionException : ViciOneServiceBusException
{
    /// <summary>Creates a rejection for a payload that exceeds the configured size limit.</summary>
    /// <param name="stage">The serialization boundary that rejected the payload.</param>
    /// <param name="actualBytes">The measured or minimally required byte count.</param>
    /// <param name="configuredLimitBytes">The configured inclusive byte limit.</param>
    /// <param name="message">A description of the rejected payload.</param>
    public PayloadAdmissionException(
        PayloadAdmissionStage stage,
        long actualBytes,
        long configuredLimitBytes,
        string message)
        : base(Validate(stage, actualBytes, configuredLimitBytes, message))
    {
        Stage = stage;
        ActualBytes = actualBytes;
        ConfiguredLimitBytes = configuredLimitBytes;
    }

    /// <summary>Gets the serialization boundary that rejected the payload.</summary>
    public PayloadAdmissionStage Stage { get; }

    /// <summary>Gets the measured or minimally required byte count.</summary>
    public long ActualBytes { get; }

    /// <summary>Gets the configured inclusive byte limit.</summary>
    public long ConfiguredLimitBytes { get; }

    private static string Validate(
        PayloadAdmissionStage stage,
        long actualBytes,
        long configuredLimitBytes,
        string message)
    {
        if (!Enum.IsDefined(stage))
            throw new ArgumentOutOfRangeException(nameof(stage), stage, "Unknown payload admission stage.");

        ArgumentOutOfRangeException.ThrowIfNegative(actualBytes);
        ArgumentOutOfRangeException.ThrowIfLessThan(configuredLimitBytes, 1);
        if (actualBytes <= configuredLimitBytes)
        {
            throw new ArgumentOutOfRangeException(
                nameof(actualBytes),
                actualBytes,
                $"Must exceed {nameof(configuredLimitBytes)} ({configuredLimitBytes}) for a rejected payload.");
        }

        ArgumentException.ThrowIfNullOrWhiteSpace(message);
        return message;
    }
}
