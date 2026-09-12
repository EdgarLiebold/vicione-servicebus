using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Runtime.Serialization;
using System.Text.Json;

namespace ViciOne.ServiceBus.Serialization;

/// <summary>Serializes an arbitrary metadata value as a reusable UTF-8 JSON body.</summary>
internal sealed class SystemTextJsonObjectMessageBody :
    MessageBody
{
    readonly byte[] _content;

    /// <summary>Creates an owned snapshot of the encoded JSON value.</summary>
    /// <param name="value">The value to encode.</param>
    /// <param name="options">The JSON serializer options used while creating the snapshot.</param>
    public SystemTextJsonObjectMessageBody(object value, JsonSerializerOptions options)
    {
        ArgumentNullException.ThrowIfNull(value);
        ArgumentNullException.ThrowIfNull(options);
        _content = Serialize(value, options);
    }

    /// <summary>Gets the exact encoded UTF-8 byte length.</summary>
    public long Length => _content.LongLength;

    /// <summary>Copies the encoded value into a new array.</summary>
    /// <returns>An independently mutable copy of the encoded value.</returns>
    public byte[] ToArray() => (byte[])_content.Clone();

    /// <summary>Opens a non-writable stream over the encoded value.</summary>
    /// <returns>A readable stream positioned at the start of the body.</returns>
    public Stream OpenReadStream() => new MemoryStream(_content, false);

    /// <summary>Tries to get the encoded value as JSON text.</summary>
    /// <param name="text">The serialized value text.</param>
    /// <returns>Always <see langword="true" />.</returns>
    public bool TryGetTransportText([NotNullWhen(true)] out string? text)
    {
        text = MessageDefaults.Encoding.GetString(_content);
        return true;
    }

    static byte[] Serialize(object value, JsonSerializerOptions options)
    {
        try
        {
            return JsonSerializer.SerializeToUtf8Bytes(value, options);
        }
        catch (Exception ex)
        {
            throw new SerializationException("Failed to serialize message", ex);
        }
    }
}
