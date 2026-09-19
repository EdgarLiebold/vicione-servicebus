using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Text;
using Apache.NMS;

namespace ViciOne.ServiceBus.ActiveMq;

/// <summary>Owns a stable snapshot of an Apache NMS text or byte message.</summary>
internal sealed class ActiveMqMessageBody :
    MessageBody,
    TransportTextMessageBody
{
    readonly byte[] _content;
    readonly string? _text;

    /// <summary>Creates an owned body snapshot from an Apache NMS message.</summary>
    /// <param name="message">The native message to snapshot.</param>
    public ActiveMqMessageBody(IMessage message)
    {
        ArgumentNullException.ThrowIfNull(message);

        switch (message)
        {
            case ITextMessage text:
                _text = text.Text ?? string.Empty;
                _content = MessageDefaults.Encoding.GetBytes(_text);
                break;

            case IBytesMessage bytes:
                byte[]? content = bytes.Content;
                _content = content is null ? [] : (byte[])content.Clone();
                break;

            case Apache.NMS.ActiveMQ.Commands.ActiveMQMessage classic
                when classic.GetType() == typeof(Apache.NMS.ActiveMQ.Commands.ActiveMQMessage):
            case Apache.NMS.AMQP.Message.NmsMessage amqp
                when amqp.GetType() == typeof(Apache.NMS.AMQP.Message.NmsMessage):
                _content = [];
                break;

            default:
                throw new ActiveMqTransportException(
                    $"The message type is not supported: {TypeCache.GetShortName(message.GetType())}");
        }
    }

    /// <summary>Gets the exact snapshot length in bytes.</summary>
    public long Length => _content.LongLength;

    /// <summary>Copies the native-message snapshot into a new array.</summary>
    /// <returns>An independently mutable copy of the snapshot.</returns>
    public byte[] ToArray() => (byte[])_content.Clone();

    /// <summary>Creates a read-only stream over the cached body bytes.</summary>
    /// <returns>A non-writable memory stream positioned at the beginning of the body.</returns>
    public Stream OpenReadStream() => new MemoryStream(_content, false);

    /// <summary>Tries to get the native text-message content without interpreting byte messages.</summary>
    /// <param name="text">The native text snapshot for a text message.</param>
    /// <returns><see langword="true" /> for a text message; otherwise, <see langword="false" />.</returns>
    public bool TryGetTransportText([NotNullWhen(true)] out string? text)
    {
        text = _text;
        return text is not null;
    }

    /// <summary>Tries to expose the native text-message payload without interpreting byte messages as text.</summary>
    /// <param name="text">The native text snapshot for an Apache NMS text message.</param>
    /// <returns><see langword="true" /> for text messages; otherwise, <see langword="false" />.</returns>
    public bool TryGetPayloadText([NotNullWhen(true)] out string? text)
    {
        text = _text;
        return text is not null;
    }
}
