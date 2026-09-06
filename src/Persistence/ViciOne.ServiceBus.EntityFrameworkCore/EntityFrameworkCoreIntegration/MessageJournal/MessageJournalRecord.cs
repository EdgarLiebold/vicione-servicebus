using System;
using System.Text.Json;
using ViciOne.ServiceBus.MessageJournal;

namespace ViciOne.ServiceBus.EntityFrameworkCore.MessageJournal;
/// <summary>
/// Relational persistence representation of a sanitized message-journal entry.
/// </summary>
public sealed class MessageJournalRecord
{
    private static readonly JsonSerializerOptions JsonOptions = JsonSerializerOptions.Web;

    /// <summary>
    /// Gets or sets the entry id value.
    /// </summary>
    public Guid EntryId { get; set; }

    /// <summary>
    /// Gets or sets the observed at value.
    /// </summary>
    public DateTimeOffset ObservedAt { get; set; }

    /// <summary>
    /// Gets or sets the operation value.
    /// </summary>
    public MessageJournalOperation Operation { get; set; }

    /// <summary>
    /// Gets or sets the outcome value.
    /// </summary>
    public MessageJournalOutcome Outcome { get; set; }

    /// <summary>
    /// Gets or sets the data classification value.
    /// </summary>
    public MessageJournalDataClassification DataClassification { get; set; }

    /// <summary>
    /// Gets or sets the content type value.
    /// </summary>
    public string? ContentType { get; set; }

    /// <summary>
    /// Gets or sets the message types json value.
    /// </summary>
    public string MessageTypesJson { get; set; } = "[]";

    /// <summary>
    /// Gets or sets the metadata json value.
    /// </summary>
    public string MetadataJson { get; set; } = "{}";

    /// <summary>
    /// Gets or sets the headers json value.
    /// </summary>
    public string HeadersJson { get; set; } = "{}";

    /// <summary>
    /// Gets or sets the body value.
    /// </summary>
    public byte[] Body { get; set; } = [];

    /// <summary>
    /// Gets or sets the content size in bytes value.
    /// </summary>
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
