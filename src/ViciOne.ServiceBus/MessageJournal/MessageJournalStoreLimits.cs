#nullable enable
namespace ViciOne.ServiceBus.MessageJournal;

using System;
using System.Threading;

/// <summary>
/// Finite limits a message-journal store guarantees to enforce transactionally on every append.
/// </summary>
/// <remarks>
/// Age-based cleanup is append-driven. An idle journal has no background worker, queue, scheduler or
/// retry carrier.
/// </remarks>
public sealed class MessageJournalStoreLimits
{
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

    public int MaximumEntryBytes { get; }

    public int MaximumEntries { get; }

    public TimeSpan RetentionPeriod { get; }
}
