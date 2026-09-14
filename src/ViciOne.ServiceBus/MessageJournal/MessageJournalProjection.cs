using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace ViciOne.ServiceBus.MessageJournal;
/// <summary>Sanitized content selected by the application policy for persistence.</summary>
public sealed class MessageJournalProjection
{
    private readonly byte[] _body;

    /// <summary>Creates an immutable snapshot of policy-approved content.</summary>
    /// <param name="dataClassification">The handling classification assigned by the policy.</param>
    /// <param name="contentType">The approved content type, or <see langword="null"/> when it is intentionally omitted.</param>
    /// <param name="messageTypes">The approved message-contract identifiers.</param>
    /// <param name="metadata">The approved envelope metadata.</param>
    /// <param name="headers">The approved transport headers.</param>
    /// <param name="body">The approved body bytes.</param>
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

    /// <summary>Gets the handling classification assigned to the sanitized content.</summary>
    public MessageJournalDataClassification DataClassification { get; }

    /// <summary>Gets the approved content type, when retained by the policy.</summary>
    public string? ContentType { get; }

    /// <summary>Gets the immutable snapshot of approved message-contract identifiers.</summary>
    public IReadOnlyList<string> MessageTypes { get; }

    /// <summary>Gets the immutable snapshot of approved envelope metadata.</summary>
    public IReadOnlyDictionary<string, string> Metadata { get; }

    /// <summary>Gets the immutable snapshot of approved transport headers.</summary>
    public IReadOnlyDictionary<string, string> Headers { get; }

    /// <summary>Gets an independently mutable copy of the approved body.</summary>
    public ReadOnlyMemory<byte> Body => _body.ToArray();

    private static IReadOnlyDictionary<string, string> Snapshot(IReadOnlyDictionary<string, string>? source)
    {
        return new ReadOnlyDictionary<string, string>(
            source is null
                ? new Dictionary<string, string>(StringComparer.Ordinal)
                : new Dictionary<string, string>(source, StringComparer.Ordinal));
    }
}
