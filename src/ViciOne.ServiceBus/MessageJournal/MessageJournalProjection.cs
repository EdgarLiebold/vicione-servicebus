using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

#nullable enable
namespace ViciOne.ServiceBus.MessageJournal;
/// <summary>
/// Sanitized content selected by the application policy for persistence.
/// </summary>
public sealed class MessageJournalProjection
{
    private readonly byte[] _body;

    public MessageJournalProjection(
        MessageJournalDataClassification dataClassification,
        string? contentType,
        IEnumerable<string>? messageTypes = null,
        IReadOnlyDictionary<string, string>? metadata = null,
        IReadOnlyDictionary<string, string>? headers = null,
        ReadOnlyMemory<byte> body = default)
    {
        if (!Enum.IsDefined(dataClassification))
            throw new ArgumentOutOfRangeException(nameof(dataClassification), dataClassification, "Unknown data classification.");

        DataClassification = dataClassification;
        ContentType = contentType;
        MessageTypes = Array.AsReadOnly((messageTypes ?? []).ToArray());
        Metadata = Snapshot(metadata);
        Headers = Snapshot(headers);
        _body = body.ToArray();
    }

    public MessageJournalDataClassification DataClassification { get; }

    public string? ContentType { get; }

    public IReadOnlyList<string> MessageTypes { get; }

    public IReadOnlyDictionary<string, string> Metadata { get; }

    public IReadOnlyDictionary<string, string> Headers { get; }

    public ReadOnlyMemory<byte> Body => _body.ToArray();

    private static IReadOnlyDictionary<string, string> Snapshot(IReadOnlyDictionary<string, string>? source)
    {
        return new ReadOnlyDictionary<string, string>(
            source is null
                ? new Dictionary<string, string>(StringComparer.Ordinal)
                : new Dictionary<string, string>(source, StringComparer.Ordinal));
    }
}
