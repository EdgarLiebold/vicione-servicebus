namespace ViciOne.ServiceBus.Serialization;

using System;
using System.IO;
using MessagePack;


public class MessagePackMessageBody<TMessage> :
    MessageBody
    where TMessage : class
{
    public long? Length => _lazyMessagePackSerializedObject.Value.Length;

    readonly Lazy<byte[]> _lazyMessagePackSerializedObject;

    public MessagePackMessageBody(SendContext<TMessage> context, MessagePackEnvelope? envelope = null)
    {
        _lazyMessagePackSerializedObject = new Lazy<byte[]>(() =>
        {
            var envelopeToSerialize = envelope ?? new MessagePackEnvelope(context, context.Message);

            return InternalMessagePackResolver.Serialize(envelopeToSerialize);
        });
    }

    public MessagePackMessageBody(TMessage message)
    {
        _lazyMessagePackSerializedObject = new Lazy<byte[]>(() => InternalMessagePackResolver.Serialize(message));
    }

    public Stream GetStream()
    {
        return new MemoryStream(_lazyMessagePackSerializedObject.Value, false);
    }

    public byte[] GetBytes()
    {
        return _lazyMessagePackSerializedObject.Value;
    }

    public string GetString()
    {
        return Convert.ToBase64String(_lazyMessagePackSerializedObject.Value);
    }
}
