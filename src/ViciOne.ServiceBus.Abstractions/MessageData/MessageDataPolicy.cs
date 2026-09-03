namespace ViciOne.ServiceBus
{
    using System;


    /// <summary>
    /// Immutable policy controlling how message data is inlined and persisted.
    /// </summary>
    public sealed record MessageDataPolicy
    {
        public static MessageDataPolicy Default { get; } = new();

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

        public bool AlwaysWriteToRepository { get; }
        public int Threshold { get; }
        public TimeSpan? TimeToLive { get; }
        public TimeSpan? ExtraTimeToLive { get; }
    }
}
