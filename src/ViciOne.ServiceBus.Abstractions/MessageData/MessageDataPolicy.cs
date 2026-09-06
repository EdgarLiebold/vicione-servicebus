using System;

namespace ViciOne.ServiceBus.Advanced.Serialization;

/// <summary>Immutable policy controlling how message data is inlined and persisted.</summary>
public sealed record MessageDataPolicy
{
    /// <summary>Gets the default.</summary>
    public static MessageDataPolicy Default { get; } = new();

    /// <summary>Initializes a new instance.</summary>
    /// <param name="alwaysWriteToRepository">The always write to repository.</param>
    /// <param name="threshold">The threshold.</param>
    /// <param name="timeToLive">The time to live.</param>
    /// <param name="extraTimeToLive">The extra time to live.</param>
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

    /// <summary>Gets the always write to repository.</summary>
    public bool AlwaysWriteToRepository { get; }
    /// <summary>Gets the threshold.</summary>
    public int Threshold { get; }
    /// <summary>Gets the time to live.</summary>
    public TimeSpan? TimeToLive { get; }
    /// <summary>Gets the extra time to live.</summary>
    public TimeSpan? ExtraTimeToLive { get; }
}
