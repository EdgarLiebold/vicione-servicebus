using System;
using System.IO;
using System.Runtime.Serialization;
using System.Text;
using System.Text.Json;

namespace ViciOne.ServiceBus.Serialization;

/// <summary>Serializes an arbitrary metadata value as a reusable UTF-8 JSON body.</summary>
internal sealed class SystemTextJsonObjectMessageBody :
    MessageBody
{
    readonly JsonSerializerOptions _options;
    readonly object _value;
    byte[]? _bytes;
    string? _string;

    /// <summary>Creates a lazily encoded JSON body.</summary>
    /// <param name="value">The value to encode.</param>
    /// <param name="options">The immutable JSON serializer options.</param>
    public SystemTextJsonObjectMessageBody(object value, JsonSerializerOptions options)
    {
        _value = value ?? throw new ArgumentNullException(nameof(value));
        _options = options ?? throw new ArgumentNullException(nameof(options));
    }

    /// <summary>Gets the exact encoded UTF-8 byte length.</summary>
    public long? Length => GetBytes().LongLength;

    /// <summary>Opens a non-writable stream over the encoded value.</summary>
    /// <returns>A readable stream positioned at the start of the body.</returns>
    public Stream GetStream()
    {
        return new MemoryStream(GetBytes(), false);
    }

    /// <summary>Gets the encoded value as UTF-8 bytes.</summary>
    /// <returns>The serialized value bytes.</returns>
    public byte[] GetBytes()
    {
        if (_bytes != null)
            return _bytes;

        if (_string != null)
        {
            _bytes = Encoding.UTF8.GetBytes(_string);
            return _bytes;
        }

        try
        {
            _bytes = JsonSerializer.SerializeToUtf8Bytes(_value, _options);

            return _bytes;
        }
        catch (Exception ex)
        {
            throw new SerializationException("Failed to serialize message", ex);
        }
    }

    /// <summary>Gets the encoded value as JSON text.</summary>
    /// <returns>The serialized value text.</returns>
    public string GetString()
    {
        if (_string != null)
            return _string;

        if (_bytes != null)
        {
            _string = Encoding.UTF8.GetString(_bytes);
            return _string;
        }

        try
        {
            _string = JsonSerializer.Serialize(_value, _options);

            return _string;
        }
        catch (Exception ex)
        {
            throw new SerializationException("Failed to serialize message", ex);
        }
    }
}
