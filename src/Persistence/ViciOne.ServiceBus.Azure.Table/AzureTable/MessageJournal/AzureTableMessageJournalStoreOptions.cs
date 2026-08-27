#nullable enable
namespace ViciOne.ServiceBus.AzureTable.MessageJournal;

using System;
using ViciOne.ServiceBus.MessageJournal;

/// <summary>
/// Binds one journal to one finite Azure Table partition so capacity and retention changes can be
/// committed atomically with each append.
/// </summary>
public sealed class AzureTableMessageJournalStoreOptions
{
    public const int MaximumBatchBoundEntries = 98;
    public const int MaximumBinaryPropertyBytes = 64 * 1024;

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

    public MessageJournalStoreLimits Limits { get; }

    public string PartitionKey { get; }
}
