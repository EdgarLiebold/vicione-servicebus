using System;
using ViciOne.ServiceBus.MessageJournal;

namespace ViciOne.ServiceBus.AzureTable.MessageJournal;
/// <summary>
/// Binds one journal to one finite Azure Table partition so capacity and retention changes can be
/// committed atomically with each append.
/// </summary>
public sealed class AzureTableMessageJournalStoreOptions
{
    /// <summary>Gets the largest entry capacity that leaves room for the lease update and appended row in one Azure Table transaction.</summary>
    public const int MaximumBatchBoundEntries = 98;
    /// <summary>Gets the Azure Table size limit applied to each binary or UTF-16 string property.</summary>
    public const int MaximumBinaryPropertyBytes = 64 * 1024;

    /// <summary>Creates validated bounds for a journal stored in one Azure Table partition.</summary>
    /// <param name="partitionKey">The partition that contains the journal lease and all journal entries.</param>
    /// <param name="limits">The finite capacity, entry-size, and retention limits.</param>
    public AzureTableMessageJournalStoreOptions(
        string partitionKey,
        MessageJournalStoreLimits limits)
    {
        ArgumentNullException.ThrowIfNull(limits);
        AzureTableKeyValidator.Validate(partitionKey, nameof(partitionKey));

        if (limits.MaximumEntries > MaximumBatchBoundEntries)
        {
            throw new ArgumentOutOfRangeException(
                nameof(limits),
                limits.MaximumEntries,
                $"An atomically bounded Azure Table journal supports at most {MaximumBatchBoundEntries} entries per partition.");
        }

        if (limits.MaximumEntryBytes > MaximumBinaryPropertyBytes)
        {
            throw new ArgumentOutOfRangeException(
                nameof(limits),
                limits.MaximumEntryBytes,
                $"Azure Table binary properties support at most {MaximumBinaryPropertyBytes} bytes.");
        }

        PartitionKey = partitionKey;
        Limits = limits;
    }

    /// <summary>Gets the finite journal capacity, entry-size, and retention limits.</summary>
    public MessageJournalStoreLimits Limits { get; }

    /// <summary>Gets the Azure Table partition shared by the journal lease and entries.</summary>
    public string PartitionKey { get; }
}
