#nullable enable
namespace ViciOne.ServiceBus.AzureTable.MessageJournal;

using System;
using System.Linq;
using System.Text;
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
        ArgumentException.ThrowIfNullOrWhiteSpace(partitionKey);
        ArgumentNullException.ThrowIfNull(limits);
        if (Encoding.Unicode.GetByteCount(partitionKey) + sizeof(int) > 1024
            || partitionKey.Any(IsDisallowedKeyCharacter))
        {
            throw new ArgumentException(
                "The Azure Table partition key must fit the 1-KiB storage boundary and contain no '/', '\\', '#', '?', or control characters.",
                nameof(partitionKey));
        }

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

    private static bool IsDisallowedKeyCharacter(char value) =>
        value is '/' or '\\' or '#' or '?'
        || value is >= '\u0000' and <= '\u001f'
        || value is >= '\u007f' and <= '\u009f';
}
