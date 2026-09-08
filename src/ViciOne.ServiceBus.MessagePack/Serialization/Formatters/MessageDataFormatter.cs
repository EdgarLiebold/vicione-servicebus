using MessagePack;
using MessagePack.Formatters;
using ViciOne.ServiceBus.Advanced.Serialization;
using ViciOne.ServiceBus.MessageData;
using ViciOne.ServiceBus.MessageData.Values;
using ViciOne.ServiceBus.Serialization.JsonConverters;

namespace ViciOne.ServiceBus.MessagePack.Serialization.Formatters;

/// <summary>Serializes message-data references and inline values through the shared reference envelope.</summary>
/// <typeparam name="T">The value exposed by the message-data handle.</typeparam>
internal sealed class MessageDataFormatter<T> :
    IMessagePackFormatter<MessageData<T>?>
{
    /// <summary>Writes the external address and, when present, the inline text or bytes of a message-data handle.</summary>
    /// <param name="writer">The MessagePack writer that receives the reference envelope.</param>
    /// <param name="value">The message-data handle to serialize, or <see langword="null"/> for an empty envelope.</param>
    /// <param name="options">The serializer options whose resolver supplies the envelope formatter.</param>
    public void Serialize(ref MessagePackWriter writer, MessageData<T>? value, MessagePackSerializerOptions options)
    {
        var reference = new SystemTextMessageDataReference();

        if (value is IMessageData { HasValue: true } messageData)
        {
            reference.Reference = messageData.Address;

            if (messageData is IInlineMessageData inlineMessageData)
                inlineMessageData.Set(reference);
        }

        IMessagePackFormatter<SystemTextMessageDataReference> innerFormatter = options.Resolver.GetFormatterWithVerify<SystemTextMessageDataReference>();

        innerFormatter.Serialize(ref writer, reference, options);
    }

    /// <summary>Reads an inline value, an external reference, or an empty message-data handle.</summary>
    /// <param name="reader">The MessagePack reader positioned at the reference envelope.</param>
    /// <param name="options">The serializer options whose resolver supplies the envelope formatter.</param>
    /// <returns>A handle for the inline value or external address, or the shared empty handle when neither is present.</returns>
    public MessageData<T>? Deserialize(ref MessagePackReader reader, MessagePackSerializerOptions options)
    {
        IMessagePackFormatter<SystemTextMessageDataReference> innerFormatter = options.Resolver.GetFormatterWithVerify<SystemTextMessageDataReference>();

        var reference = innerFormatter.Deserialize(ref reader, options);

        if (reference?.Text != null)
            return (MessageData<T>?)new StringInlineMessageData(reference.Text, reference.Reference);
        if (reference?.Data != null)
            return (MessageData<T>?)new BytesInlineMessageData(reference.Data, reference.Reference);

        if (reference?.Reference == null)
            return EmptyMessageData<T>.Instance;

        return new DeserializedMessageData<T>(reference.Reference);
    }
}
