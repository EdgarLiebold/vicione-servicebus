using System;
using System.Net.Mime;

namespace ViciOne.ServiceBus.Serialization;

/// <summary>Provides one lazily created MessagePack serializer for both send and receive operations.</summary>
public class MessagePackSerializerFactory
    : ISerializerFactory
{
    /// <summary>Gets the media type produced and consumed by the factory's serializer.</summary>
    public ContentType ContentType => MessagePackMessageSerializer.MessagePackContentType;

    readonly Lazy<MessagePackMessageSerializer> _serializer;

    /// <summary>Creates a factory whose serializer is initialized on first use.</summary>
    public MessagePackSerializerFactory()
    {
        _serializer = new Lazy<MessagePackMessageSerializer>(() => new MessagePackMessageSerializer());
    }

    /// <summary>Gets the shared MessagePack message serializer.</summary>
    /// <returns>The lazily initialized serializer used for outgoing messages.</returns>
    public IMessageSerializer CreateSerializer()
    {
        return _serializer.Value;
    }

    /// <summary>Gets the shared MessagePack message deserializer.</summary>
    /// <returns>The same lazily initialized instance used for outgoing messages.</returns>
    public IMessageDeserializer CreateDeserializer()
    {
        return _serializer.Value;
    }
}
