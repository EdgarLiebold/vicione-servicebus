using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;

namespace ViciOne.ServiceBus.Advanced.Serialization;

/// <summary>
/// Carries a binary message body that arrived as Base64 text, so a transport that cannot carry bytes
/// can still deliver one.
/// <para>
/// <see cref="TryGetTransportText" /> preserves the carrier text; <see cref="ToArray" /> and
/// <see cref="OpenReadStream" /> expose the decoded binary content.
/// </para>
/// </summary>
public sealed class Base64MessageBody :
    MessageBody
{
    readonly byte[] _content;
    readonly string _text;

    /// <summary>Creates a body from Base64 carrier text.</summary>
    /// <param name="text">The Base64 text retained as the text representation.</param>
    public Base64MessageBody(string text)
    {
        _text = text ?? throw new ArgumentNullException(nameof(text));
        _content = Convert.FromBase64String(text);
        if (!string.Equals(text, Convert.ToBase64String(_content), StringComparison.Ordinal))
            throw new FormatException("The Base64 message-body carrier must use the canonical padded representation without whitespace.");
    }

    /// <summary>Gets the decoded binary length in bytes rather than the length of the Base64 carrier text.</summary>
    public long Length => _content.LongLength;

    /// <summary>Copies the decoded binary content into a new array.</summary>
    /// <returns>An independently mutable copy of the decoded content.</returns>
    public byte[] ToArray() => (byte[])_content.Clone();

    /// <summary>Opens a non-writable stream over the decoded bytes.</summary>
    /// <returns>A readable stream positioned at the start of the decoded body.</returns>
    public Stream OpenReadStream() => new MemoryStream(_content, false);

    /// <summary>Tries to get the original Base64 carrier text.</summary>
    /// <param name="text">The text supplied to the constructor.</param>
    /// <returns>Always <see langword="true" />.</returns>
    public bool TryGetTransportText([NotNullWhen(true)] out string? text)
    {
        text = _text;
        return true;
    }
}
