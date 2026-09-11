using System.IO;
using System.Text;
using System.Threading;
using Apache.NMS;

namespace ViciOne.ServiceBus.ActiveMq;

/// <summary>Provides cached body access for Apache NMS text and byte messages.</summary>
public class ActiveMqMessageBody :
    MessageBody
{
    readonly IMessage _message;
    readonly object _gate = new();
    byte[]? _bytes;
    bool _initialized;
    string? _string;

    /// <summary>Creates a body adapter for an Apache NMS message.</summary>
    /// <param name="message">The native message whose body is exposed.</param>
    public ActiveMqMessageBody(IMessage message)
    {
        _message = message ?? throw new ArgumentNullException(nameof(message));
    }

    /// <summary>
    /// Gets the exact encoded byte length returned by <see cref="GetBytes" />. The body is initialized
    /// once so length, stream, byte, and string accessors observe the same provider snapshot.
    /// </summary>
    public long? Length
    {
        get
        {
            EnsureInitialized();
            return _bytes!.LongLength;
        }
    }

    /// <summary>Creates a read-only stream over the cached body bytes.</summary>
    /// <returns>A non-writable memory stream positioned at the beginning of the body.</returns>
    public Stream GetStream()
    {
        EnsureInitialized();
        return new MemoryStream(_bytes!, false);
    }

    /// <summary>Gets the cached body bytes.</summary>
    /// <returns>The UTF-8 bytes of a text message or a snapshot of a byte message.</returns>
    public byte[] GetBytes()
    {
        EnsureInitialized();
        return _bytes!;
    }

    /// <summary>Gets the cached body as text.</summary>
    /// <returns>The text body or the configured decoding of a byte message.</returns>
    public string GetString()
    {
        EnsureInitialized();
        return _string!;
    }

    void EnsureInitialized()
    {
        if (Volatile.Read(ref _initialized))
            return;

        lock (_gate)
        {
            if (_initialized)
                return;

            switch (_message)
            {
                case ITextMessage text:
                    _string = text.Text ?? string.Empty;
                    _bytes = Encoding.UTF8.GetBytes(_string);
                    break;

                case IBytesMessage bytes:
                    byte[]? content = bytes.Content;
                    _bytes = content is null ? [] : (byte[])content.Clone();
                    _string = MessageDefaults.Encoding.GetString(_bytes);
                    break;

                default:
                    throw new ActiveMqTransportException(
                        $"The message type is not supported: {TypeCache.GetShortName(_message.GetType())}");
            }

            Volatile.Write(ref _initialized, true);
        }
    }
}
