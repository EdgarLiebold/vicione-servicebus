using System;
using System.Threading;

namespace ViciOne.ServiceBus.MessageJournal;
/// <summary>Finite limits a message-journal store guarantees to enforce transactionally on every append.</summary>
/// <remarks>
/// Age-based cleanup is append-driven. An idle journal has no background worker, queue, scheduler or
/// retry carrier.
/// </remarks>
public sealed class MessageJournalStoreLimits
{
    /// <summary>Initializes a new instance.</summary>
    /// <param name="maximumEntryBytes">The maximum entry bytes.</param>
    /// <param name="maximumEntries">The maximum entries.</param>
    /// <param name="retentionPeriod">The retention period.</param>
    public MessageJournalStoreLimits(int maximumEntryBytes, int maximumEntries, TimeSpan retentionPeriod)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(maximumEntryBytes, 1);
        ArgumentOutOfRangeException.ThrowIfLessThan(maximumEntries, 1);

        if (retentionPeriod <= TimeSpan.Zero || retentionPeriod == Timeout.InfiniteTimeSpan)
        {
            throw new ArgumentOutOfRangeException(
                nameof(retentionPeriod),
                retentionPeriod,
                "The message-journal retention period must be finite and greater than zero.");
        }

        MaximumEntryBytes = maximumEntryBytes;
        MaximumEntries = maximumEntries;
        RetentionPeriod = retentionPeriod;
    }

    /// <summary>Gets the maximum entry bytes.</summary>
    public int MaximumEntryBytes { get; }

    /// <summary>Gets the maximum entries.</summary>
    public int MaximumEntries { get; }

    /// <summary>Gets the retention period.</summary>
    public TimeSpan RetentionPeriod { get; }
}
