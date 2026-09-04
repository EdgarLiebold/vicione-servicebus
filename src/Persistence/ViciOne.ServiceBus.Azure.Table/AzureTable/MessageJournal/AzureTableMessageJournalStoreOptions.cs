using System;
using ViciOne.ServiceBus.MessageJournal;

#nullable enable
namespace ViciOne.ServiceBus.AzureTable.MessageJournal;
/// <summary>
/// Binds one journal to one finite Azure Table partition so capacity and retention changes can be
/// committed atomically with each append.
/// </summary>
public sealed class AzureTableMessageJournalStoreOptions
{
    /// <summary>
    /// Defines the maximum batch bound entries value.
    /// </summary>
    public const int MaximumBatchBoundEntries = 98;
    /// <summary>
    /// Defines the maximum binary property bytes value.
    /// </summary>
    public const int MaximumBinaryPropertyBytes = 64 * 1024;

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="partitionKey">The partition key value.</param>
    /// <param name="limits">The limits value.</param>
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

    /// <summary>
    /// Gets the limits value.
    /// </summary>
    public MessageJournalStoreLimits Limits { get; }

    /// <summary>
    /// Gets the partition key value.
    /// </summary>
    public string PartitionKey { get; }
}
