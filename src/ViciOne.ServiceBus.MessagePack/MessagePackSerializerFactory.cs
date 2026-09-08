using System;
using System.Net.Mime;
using ViciOne.ServiceBus.Advanced.Serialization;
using ViciOne.ServiceBus.MessagePack.Serialization;

namespace ViciOne.ServiceBus.MessagePack;

/// <summary>Provides one lazily created MessagePack serializer for send and receive operations.</summary>
public sealed class MessagePackSerializerFactory :
    ISerializerFactory
{
    readonly Lazy<MessagePackMessageSerializer> _serializer = new(
        static () => new MessagePackMessageSerializer());

    /// <summary>Gets an independent media-type descriptor for MessagePack transport envelopes.</summary>
    public ContentType ContentType => new(MessagePackMessageSerializer.MediaType);

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
