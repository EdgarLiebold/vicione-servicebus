using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace ViciOne.ServiceBus.MessageJournal;

/// <summary>Immutable raw observation presented only to the explicitly configured journal policy.</summary>
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
        byte[] body)
    {
        ArgumentNullException.ThrowIfNull(body);
        Operation = operation;
        Outcome = outcome;
        ContentType = contentType;
        MessageTypes = Array.AsReadOnly(messageTypes.ToArray());
        Metadata = Snapshot(metadata);
        Headers = Snapshot(headers);
        _body = body;
    }

    /// <summary>Gets the message operation that produced this observation.</summary>
    public MessageJournalOperation Operation { get; }

    /// <summary>Gets the terminal outcome of the observed message operation.</summary>
    public MessageJournalOutcome Outcome { get; }

    /// <summary>Gets the transport content type, when the observed envelope declares one.</summary>
    public string? ContentType { get; }

    /// <summary>Gets a snapshot of the message-contract identifiers carried by the envelope.</summary>
    public IReadOnlyList<string> MessageTypes { get; }

    /// <summary>Gets a snapshot of the recognized envelope metadata keyed by <see cref="MessageJournalMetadataKeys"/>.</summary>
    public IReadOnlyDictionary<string, string> Metadata { get; }

    /// <summary>Gets a snapshot of all transport headers converted with invariant culture.</summary>
    public IReadOnlyDictionary<string, string> Headers { get; }

    /// <summary>Gets an independently mutable copy of the captured body.</summary>
    public ReadOnlyMemory<byte> Body => _body.ToArray();

    private static IReadOnlyDictionary<string, string> Snapshot(IReadOnlyDictionary<string, string> source)
    {
        return new ReadOnlyDictionary<string, string>(
            new Dictionary<string, string>(source, StringComparer.Ordinal));
    }
}
