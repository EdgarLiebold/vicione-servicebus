using System;

namespace ViciOne.ServiceBus.Advanced.Serialization;

/// <summary>Immutable policy controlling how message data is inlined and persisted.</summary>
public sealed record MessageDataPolicy
{
    /// <summary>Gets the default policy, which persists every non-null value and inlines values smaller than 4096 bytes.</summary>
    public static MessageDataPolicy Default { get; } = new();

    /// <summary>Initializes an immutable message-data storage policy.</summary>
    /// <param name="alwaysWriteToRepository">Whether values small enough to inline are also persisted.</param>
    /// <param name="threshold">The exclusive byte boundary below which supported values are inlined.</param>
    /// <param name="timeToLive">The repository lifetime used when the outgoing message has no time-to-live.</param>
    /// <param name="extraTimeToLive">The additional repository lifetime added to an outgoing message time-to-live.</param>
    /// <exception cref="ArgumentOutOfRangeException">A duration is not positive or <paramref name="threshold" /> is negative.</exception>
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

    /// <summary>Gets whether inline values are also persisted.</summary>
    public bool AlwaysWriteToRepository { get; }
    /// <summary>Gets the exclusive byte boundary below which supported values are inlined.</summary>
    public int Threshold { get; }
    /// <summary>Gets the repository lifetime used when the outgoing message has no time-to-live.</summary>
    public TimeSpan? TimeToLive { get; }
    /// <summary>Gets the additional repository lifetime added to an outgoing message time-to-live.</summary>
    public TimeSpan? ExtraTimeToLive { get; }
}
