using System;

namespace ViciOne.ServiceBus.Advanced.Serialization;

/// <summary>
/// Immutable policy controlling how message data is inlined and persisted.
/// </summary>
public sealed record MessageDataPolicy
{
    /// <summary>
    /// Gets the default value.
    /// </summary>
    public static MessageDataPolicy Default { get; } = new();

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="alwaysWriteToRepository">The always write to repository value.</param>
    /// <param name="threshold">The threshold value.</param>
    /// <param name="timeToLive">The time to live value.</param>
    /// <param name="extraTimeToLive">The extra time to live value.</param>
    public MessageDataPolicy(
        bool alwaysWriteToRepository = true,
        int threshold = 4096,
        TimeSpan? timeToLive = null,
        TimeSpan? extraTimeToLive = null)
    {
        if (threshold < 0)
            throw new ArgumentOutOfRangeException(nameof(threshold));
        if (timeToLive.HasValue && timeToLive.Value <= TimeSpan.Zero)
            throw new ArgumentOutOfRangeException(nameof(timeToLive));
        if (extraTimeToLive.HasValue && extraTimeToLive.Value <= TimeSpan.Zero)
            throw new ArgumentOutOfRangeException(nameof(extraTimeToLive));

        AlwaysWriteToRepository = alwaysWriteToRepository;
        Threshold = threshold;
        TimeToLive = timeToLive;
        ExtraTimeToLive = extraTimeToLive;
    }

    /// <summary>
    /// Gets the always write to repository value.
    /// </summary>
    public bool AlwaysWriteToRepository { get; }
    /// <summary>
    /// Gets the threshold value.
    /// </summary>
    public int Threshold { get; }
    /// <summary>
    /// Gets the time to live value.
    /// </summary>
    public TimeSpan? TimeToLive { get; }
    /// <summary>
    /// Gets the extra time to live value.
    /// </summary>
    public TimeSpan? ExtraTimeToLive { get; }
}
