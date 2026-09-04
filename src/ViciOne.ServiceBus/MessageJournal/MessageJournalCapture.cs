using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

#nullable enable
namespace ViciOne.ServiceBus.MessageJournal;
/// <summary>
/// Immutable raw observation presented only to the explicitly configured journal policy.
/// </summary>
/// <remarks>
/// A capture may contain sensitive payload and header data. It is never passed to a persistence
/// provider. The policy must return a sanitized <see cref="MessageJournalProjection"/> or reject
/// the capture by returning <see langword="null"/>.
/// </remarks>
public sealed class MessageJournalCapture
{
    private readonly byte[] _body;

    internal MessageJournalCapture(
        MessageJournalOperation operation,
        MessageJournalOutcome outcome,
        string? contentType,
        IEnumerable<string> messageTypes,
        IReadOnlyDictionary<string, string> metadata,
        IReadOnlyDictionary<string, string> headers,
        ReadOnlyMemory<byte> body)
    {
        Operation = operation;
        Outcome = outcome;
        ContentType = contentType;
        MessageTypes = Array.AsReadOnly(messageTypes.ToArray());
        Metadata = Snapshot(metadata);
        Headers = Snapshot(headers);
        _body = body.ToArray();
    }

    public MessageJournalOperation Operation { get; }

    public MessageJournalOutcome Outcome { get; }

    public string? ContentType { get; }

    public IReadOnlyList<string> MessageTypes { get; }

    public IReadOnlyDictionary<string, string> Metadata { get; }

    public IReadOnlyDictionary<string, string> Headers { get; }

    public ReadOnlyMemory<byte> Body => _body.ToArray();

    private static IReadOnlyDictionary<string, string> Snapshot(IReadOnlyDictionary<string, string> source)
    {
        return new ReadOnlyDictionary<string, string>(
            new Dictionary<string, string>(source, StringComparer.Ordinal));
    }
}
