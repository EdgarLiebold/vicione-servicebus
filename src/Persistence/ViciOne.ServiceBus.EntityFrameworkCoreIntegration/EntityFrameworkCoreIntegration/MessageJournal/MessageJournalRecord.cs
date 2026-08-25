#nullable enable
namespace ViciOne.ServiceBus.EntityFrameworkCoreIntegration.MessageJournal;

using System;
using System.Text.Json;
using ViciOne.ServiceBus.MessageJournal;

/// <summary>
/// Relational persistence representation of a sanitized message-journal entry.
/// </summary>
public sealed class MessageJournalRecord
{
    private static readonly JsonSerializerOptions JsonOptions = JsonSerializerOptions.Web;

    public Guid EntryId { get; set; }

    public DateTimeOffset ObservedAt { get; set; }

    public MessageJournalOperation Operation { get; set; }

    public MessageJournalOutcome Outcome { get; set; }

    public MessageJournalDataClassification DataClassification { get; set; }

    public string? ContentType { get; set; }

    public string MessageTypesJson { get; set; } = "[]";

    public string MetadataJson { get; set; } = "{}";

    public string HeadersJson { get; set; } = "{}";

    public byte[] Body { get; set; } = [];

    public int ContentSizeInBytes { get; set; }

    internal static MessageJournalRecord FromEntry(MessageJournalEntry entry)
    {
        return new MessageJournalRecord
        {
            EntryId = entry.EntryId,
            ObservedAt = entry.ObservedAt,
            Operation = entry.Operation,
            Outcome = entry.Outcome,
            DataClassification = entry.DataClassification,
            ContentType = entry.ContentType,
            MessageTypesJson = JsonSerializer.Serialize(entry.MessageTypes, JsonOptions),
            MetadataJson = JsonSerializer.Serialize(entry.Metadata, JsonOptions),
            HeadersJson = JsonSerializer.Serialize(entry.Headers, JsonOptions),
            Body = entry.Body.ToArray(),
            ContentSizeInBytes = entry.ContentSizeInBytes,
        };
    }

}
