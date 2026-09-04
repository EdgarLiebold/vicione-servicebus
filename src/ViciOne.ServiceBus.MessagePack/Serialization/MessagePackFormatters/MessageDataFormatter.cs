using MessagePack;
using MessagePack.Formatters;
using ViciOne.ServiceBus.MessageData.Values;
using ViciOne.ServiceBus.Serialization.JsonConverters;

namespace ViciOne.ServiceBus.Serialization.MessagePackFormatters;

/// <summary>
/// Provides a message data formatter implementation.
/// </summary>
/// <typeparam name="T">The t type.</typeparam>
public class MessageDataFormatter<T> :
    IMessagePackFormatter<MessageData<T>?>
{
    /// <summary>
    /// Performs the serialize operation.
    /// </summary>
    /// <param name="writer">The writer value.</param>
    /// <param name="value">The value.</param>
    /// <param name="options">The options value.</param>
    public void Serialize(ref MessagePackWriter writer, MessageData<T>? value, MessagePackSerializerOptions options)
    {
        var reference = new SystemTextMessageDataReference { Reference = value?.Address };

        // Borrows System.Text.Json's SystemTextMessageDataReference type.
        IMessagePackFormatter<SystemTextMessageDataReference> innerFormatter = options.Resolver.GetFormatterWithVerify<SystemTextMessageDataReference>();

        innerFormatter.Serialize(ref writer, reference, options);
    }

    /// <summary>
    /// Performs the deserialize operation.
    /// </summary>
    /// <param name="reader">The reader value.</param>
    /// <param name="options">The options value.</param>
    /// <returns>The result of the operation.</returns>
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
