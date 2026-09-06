using System;
using System.IO;
using System.Runtime.Serialization;
using System.Text;
using System.Text.Json;

namespace ViciOne.ServiceBus.Serialization;

/// <summary>
/// Provides a system text json object message body implementation.
/// </summary>
public class SystemTextJsonObjectMessageBody :
    MessageBody
{
    readonly JsonSerializerOptions _options;
    readonly object _value;
    byte[]? _bytes;
    string? _string;

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="value">The value.</param>
    /// <param name="options">The options value.</param>
    public SystemTextJsonObjectMessageBody(object value, JsonSerializerOptions options)
    {
        _value = value;
        _options = options;
    }

    /// <summary>
    /// The number of bytes this body transmits, which is by definition the length of what
    /// <see cref="GetBytes" /> returns, whichever accessor ran first. Answering from whichever
    /// representation happened to exist reported a character count after a string read and nothing
    /// at all before the first read, so the same body gave three different answers.
    /// </summary>
    public long? Length => GetBytes().LongLength;

    /// <summary>
    /// Gets stream.
    /// </summary>
    /// <returns>The result of the operation.</returns>
    public Stream GetStream()
    {
        return new MemoryStream(GetBytes(), false);
    }

    /// <summary>
    /// Gets bytes.
    /// </summary>
    /// <returns>The result of the operation.</returns>
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

    /// <summary>
    /// Gets string.
    /// </summary>
    /// <returns>The result of the operation.</returns>
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
