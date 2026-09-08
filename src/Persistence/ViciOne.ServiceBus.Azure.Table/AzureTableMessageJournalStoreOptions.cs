using System;
using ViciOne.ServiceBus.Azure.Table.Infrastructure;
using ViciOne.ServiceBus.MessageJournal;

namespace ViciOne.ServiceBus.Azure.Table;

/// <summary>
/// Binds one journal to one finite Azure Table partition so capacity and retention changes can be
/// committed atomically with each append.
/// </summary>
public sealed class AzureTableMessageJournalStoreOptions
{
    /// <summary>Gets the largest entry capacity that leaves room for the lease update and appended row in one Azure Table transaction.</summary>
    public const int MaximumJournalEntriesPerPartition = AzureTableStorageLimits.MaximumTransactionOperations - 2;
    /// <summary>Gets the Azure Table size limit applied to each binary or UTF-16 string property.</summary>
    public const int MaximumPropertyBytes = AzureTableStorageLimits.MaximumPropertyBytes;

    /// <summary>Creates validated bounds for a journal stored in one Azure Table partition.</summary>
    /// <param name="partitionKey">The partition that contains the journal lease and all journal entries.</param>
    /// <param name="limits">The finite capacity, entry-size, and retention limits.</param>
    /// <exception cref="ArgumentNullException"><paramref name="partitionKey"/> or <paramref name="limits"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException"><paramref name="partitionKey"/> is empty or invalid for Azure Table.</exception>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="limits"/> exceeds the atomic transaction or property-size boundary.</exception>
    public AzureTableMessageJournalStoreOptions(
        string partitionKey,
        MessageJournalStoreLimits limits)
    {
        ArgumentNullException.ThrowIfNull(limits);
        AzureTableKeyValidator.Validate(partitionKey, nameof(partitionKey));

        if (limits.MaximumEntries > MaximumJournalEntriesPerPartition)
        {
            throw new ArgumentOutOfRangeException(
                nameof(limits),
                limits.MaximumEntries,
                $"An atomically bounded Azure Table journal supports at most {MaximumJournalEntriesPerPartition} entries per partition.");
        }

        if (limits.MaximumEntryBytes > MaximumPropertyBytes)
        {
            throw new ArgumentOutOfRangeException(
                nameof(limits),
                limits.MaximumEntryBytes,
                $"Azure Table properties used by the journal support at most {MaximumPropertyBytes} bytes.");
        }

        PartitionKey = partitionKey;
        Limits = limits;
    }

    /// <summary>Gets the finite journal capacity, entry-size, and retention limits.</summary>
    public MessageJournalStoreLimits Limits { get; }

    /// <summary>Gets the Azure Table partition shared by the journal lease and entries.</summary>
    public string PartitionKey { get; }
}
