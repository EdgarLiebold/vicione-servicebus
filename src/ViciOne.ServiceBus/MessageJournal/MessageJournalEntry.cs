using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Text.Json;

#nullable enable
namespace ViciOne.ServiceBus.MessageJournal;
/// <summary>
/// Immutable, sanitized message-journal entry delivered to a persistence provider.
/// </summary>
public sealed class MessageJournalEntry
{
    private readonly byte[] _body;

    internal MessageJournalEntry(
        Guid entryId,
        DateTimeOffset observedAt,
        MessageJournalOperation operation,
        MessageJournalOutcome outcome,
        MessageJournalProjection projection)
    {
        EntryId = entryId;
        ObservedAt = observedAt;
        Operation = operation;
        Outcome = outcome;
        DataClassification = projection.DataClassification;
        ContentType = projection.ContentType;
        MessageTypes = Array.AsReadOnly(projection.MessageTypes.ToArray());
        Metadata = Snapshot(projection.Metadata);
        Headers = Snapshot(projection.Headers);
        _body = projection.Body.ToArray();
        ContentSizeInBytes = CalculateContentSize();
    }

    public Guid EntryId { get; }

    public DateTimeOffset ObservedAt { get; }

    public MessageJournalOperation Operation { get; }

    public MessageJournalOutcome Outcome { get; }

    public MessageJournalDataClassification DataClassification { get; }

    public string? ContentType { get; }

    public IReadOnlyList<string> MessageTypes { get; }

    public IReadOnlyDictionary<string, string> Metadata { get; }

    public IReadOnlyDictionary<string, string> Headers { get; }

    public ReadOnlyMemory<byte> Body => _body.ToArray();

    /// <summary>
    /// Conservative UTF-8 content size used for the store's fail-closed entry-size boundary.
    /// </summary>
    public int ContentSizeInBytes { get; }

    private int CalculateContentSize()
    {
        long size = 128L + _body.Length + Utf8Length(ContentType);
        size += JsonArrayUtf8Length(MessageTypes);
        size += JsonObjectUtf8Length(Metadata);
        size += JsonObjectUtf8Length(Headers);

        return size > int.MaxValue ? int.MaxValue : (int)size;
    }

    private static long JsonArrayUtf8Length(IReadOnlyList<string> values)
    {
        long size = 2;
        for (var index = 0; index < values.Count; index++)
        {
            if (index > 0)
                size++;

            size += JsonStringUtf8Length(values[index]);
        }

        return size;
    }

    private static long JsonObjectUtf8Length(IReadOnlyDictionary<string, string> values)
    {
        long size = 2;
        var index = 0;
        foreach ((string key, string value) in values)
        {
            if (index++ > 0)
                size++;

            size += JsonStringUtf8Length(key) + 1 + JsonStringUtf8Length(value);
        }

        return size;
    }

    private static long JsonStringUtf8Length(string value) =>
        JsonEncodedText.Encode(value).EncodedUtf8Bytes.Length + 2L;

    private static int Utf8Length(string? value) => value is null ? 0 : Encoding.UTF8.GetByteCount(value);

    private static IReadOnlyDictionary<string, string> Snapshot(IReadOnlyDictionary<string, string> source)
    {
        return new ReadOnlyDictionary<string, string>(
            new Dictionary<string, string>(source, StringComparer.Ordinal));
    }
}
