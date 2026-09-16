using System;
using System.Text.Json;
using ViciOne.ServiceBus.MessageJournal;

namespace ViciOne.ServiceBus.EntityFrameworkCore.MessageJournal;

/// <summary>Relational persistence representation of a sanitized message-journal entry.</summary>
public sealed class MessageJournalRecord
{
    private static readonly JsonSerializerOptions JsonOptions = JsonSerializerOptions.Web;

    /// <summary>Gets or sets the journal entry identifier.</summary>
    public Guid EntryId { get; set; }

    /// <summary>Gets or sets the UTC time when the operation was observed.</summary>
    public DateTimeOffset ObservedAt { get; set; }

    /// <summary>Gets or sets whether the entry represents a send, publish, or receive observation.</summary>
    public MessageJournalOperation Operation { get; set; }

    /// <summary>Gets or sets the observed operation outcome.</summary>
    public MessageJournalOutcome Outcome { get; set; }

    /// <summary>Gets or sets the data classification applied before persistence.</summary>
    public MessageJournalDataClassification DataClassification { get; set; }

    /// <summary>Gets or sets the sanitized message content type.</summary>
    public string? ContentType { get; set; }

    /// <summary>Gets or sets the JSON array of sanitized message-type identifiers.</summary>
    public string MessageTypesJson { get; set; } = "[]";

    /// <summary>Gets or sets the JSON object containing sanitized envelope metadata.</summary>
    public string MetadataJson { get; set; } = "{}";

    /// <summary>Gets or sets the JSON object containing sanitized headers.</summary>
    public string HeadersJson { get; set; } = "{}";

    /// <summary>Gets or sets the captured sanitized message body.</summary>
    public byte[] Body { get; set; } = [];

    /// <summary>Gets or sets the combined persisted content size used for entry-limit enforcement.</summary>
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
