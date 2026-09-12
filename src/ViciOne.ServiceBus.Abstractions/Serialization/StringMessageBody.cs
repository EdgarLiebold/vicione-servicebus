using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;

namespace ViciOne.ServiceBus.Advanced.Serialization;

/// <summary>Exposes text and its UTF-8 encoding as a message body.</summary>
public sealed class StringMessageBody :
    MessageBody,
    TransportTextMessageBody
{
    readonly string _body;
    readonly byte[] _content;
    readonly int _length;

    /// <summary>Creates a body from text.</summary>
    /// <param name="body">The complete body text.</param>
    public StringMessageBody(string body)
    {
        _body = body ?? throw new ArgumentNullException(nameof(body));
        _content = MessageDefaults.Encoding.GetBytes(body);
        _length = _content.Length;
    }

    private StringMessageBody(byte[] content, int length)
    {
        _content = content;
        _length = length;
        _body = MessageDefaults.Encoding.GetString(content, 0, length);
    }

    /// <summary>Gets the UTF-8 encoded body length in bytes.</summary>
    public long Length => _length;

    /// <summary>Copies the UTF-8 content into a new array.</summary>
    /// <returns>An independently mutable copy of the encoded text.</returns>
    public byte[] ToArray() => _content.AsSpan(0, _length).ToArray();

    /// <summary>Opens a non-writable stream over the cached UTF-8 bytes.</summary>
    /// <returns>A readable stream positioned at the start of the body.</returns>
    public Stream OpenReadStream() => new MemoryStream(_content, 0, _length, false, false);

    /// <summary>Gets the capacity of the owned UTF-8 buffer retained by this body.</summary>
    internal int OwnedCapacity => _content.Length;

    /// <summary>Tries to get the original or strictly decoded UTF-8 text.</summary>
    /// <param name="text">The complete body text.</param>
    /// <returns>Always <see langword="true" />.</returns>
    public bool TryGetTransportText([NotNullWhen(true)] out string? text)
    {
        text = _body;
        return true;
    }

    /// <summary>Exposes the string as serializer payload text.</summary>
    /// <param name="text">The original string.</param>
    /// <returns>Always <see langword="true" />.</returns>
    public bool TryGetPayloadText([NotNullWhen(true)] out string? text)
    {
        text = _body;
        return true;
    }

    /// <summary>Transfers an owned UTF-8 buffer and exposes only its serialized prefix.</summary>
    internal static StringMessageBody TakeUtf8Ownership(byte[] content, int length)
    {
        ArgumentNullException.ThrowIfNull(content);
        ArgumentOutOfRangeException.ThrowIfNegative(length);
        if (length > content.Length)
            throw new ArgumentOutOfRangeException(nameof(length), length, "The text length must not exceed the buffer length.");

        return new StringMessageBody(content, length);
    }
}
