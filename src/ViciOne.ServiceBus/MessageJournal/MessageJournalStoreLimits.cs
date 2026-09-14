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
    /// <summary>Defines the finite limits a store applies transactionally to every append.</summary>
    /// <param name="maximumEntryBytes">The largest accepted value of <see cref="MessageJournalEntry.ContentSizeInBytes"/>.</param>
    /// <param name="maximumEntries">The largest number of entries retained after an append completes.</param>
    /// <param name="retentionPeriod">The maximum age retained when a subsequent append performs cleanup.</param>
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

    /// <summary>Gets the largest accepted conservative serialized-entry size in bytes.</summary>
    public int MaximumEntryBytes { get; }

    /// <summary>Gets the largest number of entries retained after each append.</summary>
    public int MaximumEntries { get; }

    /// <summary>Gets the append-driven retention period.</summary>
    public TimeSpan RetentionPeriod { get; }
}
