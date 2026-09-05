namespace ViciOne.ServiceBus;

/// <summary>
/// Defines mandatory serialization and transport-envelope limits for one bus.
/// </summary>
public sealed record MessageLimits
{
    /// <summary>
    /// Gets a conservative immutable policy with a 1 MiB body, 2 MiB envelope, and JSON depth of 32.
    /// </summary>
    public static MessageLimits Conservative { get; } = new()
    {
        MaxBodyBytes = 1024 * 1024,
        MaxEnvelopeBytes = 2 * 1024 * 1024,
        MaxJsonDepth = 32,
    };

    /// <summary>
    /// Gets the largest serialized application body accepted for send or receive.
    /// </summary>
    public required int MaxBodyBytes { get; init; }

    /// <summary>
    /// Gets the largest final transport envelope accepted for send.
    /// </summary>
    public required int MaxEnvelopeBytes { get; init; }

    /// <summary>
    /// Gets the maximum JSON object or array nesting depth accepted by System.Text.Json.
    /// </summary>
    public required int MaxJsonDepth { get; init; }

    /// <summary>
    /// Gets the optional observation threshold for large serialized bodies.
    /// </summary>
    public int? WarnAboveBytes { get; init; }

    /// <summary>
    /// Gets the optional threshold above which message data must be offloaded to the configured repository.
    /// </summary>
    public int? OffloadToMessageDataAboveBytes { get; init; }

    internal MessageLimits Validate(string busName)
    {
        if (MaxBodyBytes <= 0)
        {
            throw new ConfigurationException(
                $"Message limits for bus '{busName}': {nameof(MaxBodyBytes)} must be greater than zero. Set {nameof(MaxBodyBytes)} to an explicit positive byte limit.");
        }

        if (MaxEnvelopeBytes <= 0)
        {
            throw new ConfigurationException(
                $"Message limits for bus '{busName}': {nameof(MaxEnvelopeBytes)} must be greater than zero. Set {nameof(MaxEnvelopeBytes)} to an explicit positive byte limit.");
        }

        if (MaxEnvelopeBytes < MaxBodyBytes)
        {
            throw new ConfigurationException(
                $"Message limits for bus '{busName}': {nameof(MaxEnvelopeBytes)} ({MaxEnvelopeBytes}) must not be less than {nameof(MaxBodyBytes)} ({MaxBodyBytes}). Set {nameof(MaxEnvelopeBytes)} to at least {nameof(MaxBodyBytes)}.");
        }

        if (MaxJsonDepth <= 0)
        {
            throw new ConfigurationException(
                $"Message limits for bus '{busName}': {nameof(MaxJsonDepth)} must be greater than zero. Set {nameof(MaxJsonDepth)} to an explicit positive nesting depth.");
        }

        ValidateOptionalThreshold(busName, WarnAboveBytes, nameof(WarnAboveBytes));
        ValidateOptionalThreshold(busName, OffloadToMessageDataAboveBytes, nameof(OffloadToMessageDataAboveBytes));
        return this;
    }

    void ValidateOptionalThreshold(string busName, int? value, string propertyName)
    {
        if (value <= 0)
        {
            throw new ConfigurationException(
                $"Message limits for bus '{busName}': {propertyName} must be greater than zero when declared. Remove it or set an explicit positive byte threshold.");
        }

        if (value > MaxBodyBytes)
        {
            throw new ConfigurationException(
                $"Message limits for bus '{busName}': {propertyName} ({value}) must not exceed {nameof(MaxBodyBytes)} ({MaxBodyBytes}). Set {propertyName} to at most {nameof(MaxBodyBytes)}.");
        }
    }
}
