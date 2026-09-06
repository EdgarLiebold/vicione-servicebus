using System;
using System.Text;
using System.Text.Json;
using Azure;
using Azure.Data.Tables;
using ViciOne.ServiceBus.MessageJournal;

namespace ViciOne.ServiceBus.AzureTable.MessageJournal;
/// <summary>Azure Table representation of one sanitized message-journal entry.</summary>
public sealed class MessageJournalRecord : ITableEntity
{
    internal const string RowKeyPrefix = "entry|";
    internal const string RowKeyUpperBound = "entry}";

    internal static readonly DateTimeOffset MinimumSupportedObservedAt =
        new(1601, 1, 1, 0, 0, 0, TimeSpan.Zero);

    private static readonly JsonSerializerOptions JsonOptions = JsonSerializerOptions.Web;

    /// <summary>Gets or sets the journal partition key.</summary>
    public string PartitionKey { get; set; } = "";

    /// <summary>Gets or sets the reverse-time row key used to order journal entries newest first.</summary>
    public string RowKey { get; set; } = "";

    /// <summary>Gets or sets the service-maintained entity modification timestamp.</summary>
    public DateTimeOffset? Timestamp { get; set; }

    /// <summary>Gets or sets the entity tag used by Azure Table concurrency control.</summary>
    public ETag ETag { get; set; }

    /// <summary>Gets or sets the stable journal-entry identifier.</summary>
    public Guid EntryId { get; set; }

    /// <summary>Gets or sets the UTC instant at which the journal observation occurred.</summary>
    public DateTimeOffset ObservedAt { get; set; }

    /// <summary>Gets or sets the serialized journal operation name.</summary>
    public string Operation { get; set; } = "";

    /// <summary>Gets or sets the serialized journal outcome name.</summary>
    public string Outcome { get; set; } = "";

    /// <summary>Gets or sets the serialized data-classification name.</summary>
    public string DataClassification { get; set; } = "";

    /// <summary>Gets or sets the media type of the sanitized body, when one was recorded.</summary>
    public string? ContentType { get; set; }

    /// <summary>Gets or sets the JSON array of message contract identities.</summary>
    public string MessageTypesJson { get; set; } = "[]";

    /// <summary>Gets or sets the sanitized journal metadata as JSON.</summary>
    public string MetadataJson { get; set; } = "{}";

    /// <summary>Gets or sets the sanitized message headers as JSON.</summary>
    public string HeadersJson { get; set; } = "{}";

    /// <summary>Gets or sets the sanitized message body.</summary>
    public byte[] Body { get; set; } = [];

    /// <summary>Gets or sets the combined byte size used for journal entry-limit enforcement.</summary>
    public int ContentSizeInBytes { get; set; }

    internal static MessageJournalRecord FromEntry(MessageJournalEntry entry, string partitionKey)
    {
        if (entry.ObservedAt < MinimumSupportedObservedAt)
        {
            throw new ArgumentOutOfRangeException(
                nameof(entry),
                entry.ObservedAt,
                $"Azure Table timestamps cannot be earlier than {MinimumSupportedObservedAt:O}.");
        }

        long reverseUtcTicks = DateTimeOffset.MaxValue.UtcTicks - entry.ObservedAt.UtcTicks;
        var record = new MessageJournalRecord
        {
            PartitionKey = partitionKey,
            RowKey = $"{RowKeyPrefix}{reverseUtcTicks:D19}|{entry.EntryId:N}",
            EntryId = entry.EntryId,
            ObservedAt = entry.ObservedAt,
            Operation = entry.Operation.ToString(),
            Outcome = entry.Outcome.ToString(),
            DataClassification = entry.DataClassification.ToString(),
            ContentType = entry.ContentType,
            MessageTypesJson = JsonSerializer.Serialize(entry.MessageTypes, JsonOptions),
            MetadataJson = JsonSerializer.Serialize(entry.Metadata, JsonOptions),
            HeadersJson = JsonSerializer.Serialize(entry.Headers, JsonOptions),
            Body = entry.Body.ToArray(),
            ContentSizeInBytes = entry.ContentSizeInBytes,
        };

        ValidateAzurePropertyLimits(record);
        return record;
    }

    private static void ValidateAzurePropertyLimits(MessageJournalRecord record)
    {
        if (record.Body.Length > AzureTableMessageJournalStoreOptions.MaximumBinaryPropertyBytes)
            throw new ArgumentOutOfRangeException(nameof(record), "The journal body exceeds the Azure Table binary property limit.");

        ValidateStringProperty(record.ContentType, nameof(ContentType));
        ValidateStringProperty(record.MessageTypesJson, nameof(MessageTypesJson));
        ValidateStringProperty(record.MetadataJson, nameof(MetadataJson));
        ValidateStringProperty(record.HeadersJson, nameof(HeadersJson));
    }

    private static void ValidateStringProperty(string? value, string propertyName)
    {
        if (value is not null
            && Encoding.Unicode.GetByteCount(value) > AzureTableMessageJournalStoreOptions.MaximumBinaryPropertyBytes)
        {
            throw new ArgumentOutOfRangeException(
                propertyName,
                "The journal value exceeds the Azure Table string property limit.");
        }
    }
}
