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

    /// <summary>
    /// Gets the operation value.
    /// </summary>
    public MessageJournalOperation Operation { get; }

    /// <summary>
    /// Gets the outcome value.
    /// </summary>
    public MessageJournalOutcome Outcome { get; }

    /// <summary>
    /// Gets the content type value.
    /// </summary>
    public string? ContentType { get; }

    /// <summary>
    /// Gets the message types value.
    /// </summary>
    public IReadOnlyList<string> MessageTypes { get; }

    /// <summary>
    /// Gets the metadata value.
    /// </summary>
    public IReadOnlyDictionary<string, string> Metadata { get; }

    /// <summary>
    /// Gets the headers value.
    /// </summary>
    public IReadOnlyDictionary<string, string> Headers { get; }

    /// <summary>
    /// Gets the body value.
    /// </summary>
    public ReadOnlyMemory<byte> Body => _body.ToArray();

    private static IReadOnlyDictionary<string, string> Snapshot(IReadOnlyDictionary<string, string> source)
    {
        return new ReadOnlyDictionary<string, string>(
            new Dictionary<string, string>(source, StringComparer.Ordinal));
    }
}
