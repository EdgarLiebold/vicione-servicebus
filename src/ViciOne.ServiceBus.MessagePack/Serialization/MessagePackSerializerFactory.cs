using System;
using System.Net.Mime;

namespace ViciOne.ServiceBus.Serialization;

public class MessagePackSerializerFactory
    : ISerializerFactory
{
    public ContentType ContentType => MessagePackMessageSerializer.MessagePackContentType;

    readonly Lazy<MessagePackMessageSerializer> _serializer;

    public MessagePackSerializerFactory()
    {
        _serializer = new Lazy<MessagePackMessageSerializer>(() => new MessagePackMessageSerializer());
    }

    public IMessageSerializer CreateSerializer()
    {
        return _serializer.Value;
    }

    public IMessageDeserializer CreateDeserializer()
    {
        return _serializer.Value;
    }
}
