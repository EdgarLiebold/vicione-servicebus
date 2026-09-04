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

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="dataClassification">The data classification value.</param>
    /// <param name="contentType">The content type value.</param>
    /// <param name="messageTypes">The message types value.</param>
    /// <param name="metadata">The metadata value.</param>
    /// <param name="headers">The headers value.</param>
    /// <param name="body">The body value.</param>
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

    /// <summary>
    /// Gets the data classification value.
    /// </summary>
    public MessageJournalDataClassification DataClassification { get; }

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

    private static IReadOnlyDictionary<string, string> Snapshot(IReadOnlyDictionary<string, string>? source)
    {
        return new ReadOnlyDictionary<string, string>(
            source is null
                ? new Dictionary<string, string>(StringComparer.Ordinal)
                : new Dictionary<string, string>(source, StringComparer.Ordinal));
    }
}
