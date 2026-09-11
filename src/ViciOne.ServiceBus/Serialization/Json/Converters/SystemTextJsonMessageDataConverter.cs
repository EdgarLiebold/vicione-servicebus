using System;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using ViciOne.ServiceBus.Internals;
using ViciOne.ServiceBus.MessageData;
using ViciOne.ServiceBus.MessageData.Converters;
using ViciOne.ServiceBus.MessageData.Values;
using ViciOne.ServiceBus.Metadata;

namespace ViciOne.ServiceBus.Serialization.Json.Converters;

/// <summary>Creates JSON converters for inline and externally referenced message-data values.</summary>
internal sealed class SystemTextJsonMessageDataConverter :
    JsonConverterFactory
{
    /// <summary>Determines whether a type is a closed <see cref="MessageData{T}" /> contract.</summary>
    /// <param name="typeToConvert">The candidate type.</param>
    /// <returns><see langword="true" /> for a closed message-data contract; otherwise, <see langword="false" />.</returns>
    public override bool CanConvert(Type typeToConvert)
    {
        ArgumentNullException.ThrowIfNull(typeToConvert);
        return typeToConvert.ClosesGenericType(typeof(MessageData<>));
    }

    /// <summary>Creates a converter for the message-data value category.</summary>
    /// <param name="typeToConvert">The closed message-data contract.</param>
    /// <param name="options">The active serializer options.</param>
    /// <returns>A converter for inline scalar, stream, binary, or object message data.</returns>
    public override JsonConverter? CreateConverter(Type typeToConvert, JsonSerializerOptions options)
    {
        ArgumentNullException.ThrowIfNull(typeToConvert);
        ArgumentNullException.ThrowIfNull(options);

        if (!typeToConvert.TryGetSingleClosedGenericArguments(typeof(MessageData<>), out Type[] types))
            return null;

        var elementType = types[0];

        if (elementType == typeof(string)
            || elementType == typeof(byte[])
            || elementType == typeof(Stream))
            return (JsonConverter)(Activator.CreateInstance(typeof(MessageDataConverter<>).MakeGenericType(types)) ?? throw new System.InvalidOperationException("The requested runtime type could not be activated."));

        if (TypeMetadataCache.IsValidMessageDataType(elementType))
            return (JsonConverter)(Activator.CreateInstance(typeof(MessageDataObjectConverter<>).MakeGenericType(types)) ?? throw new System.InvalidOperationException("The requested runtime type could not be activated."));

        throw new MessageDataException("The message data type is not supported: " + TypeCache.GetShortName(elementType));
    }


    sealed class MessageDataConverter<T> :
        JsonConverter<MessageData<T>>
    {
        public override MessageData<T> Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            var reference = JsonSerializer.Deserialize<JsonMessageDataReference>(ref reader, options);
            if (reference?.Text != null)
                return (MessageData<T>)new StringInlineMessageData(reference.Text, reference.Reference);
            if (reference?.Data != null)
                return (MessageData<T>)new BytesInlineMessageData(reference.Data, reference.Reference);

            if (reference?.Reference == null)
                return EmptyMessageData<T>.Instance;

            return new DeserializedMessageData<T>(reference.Reference);
        }

        public override void Write(Utf8JsonWriter writer, MessageData<T> value, JsonSerializerOptions options)
        {
            var reference = new JsonMessageDataReference();

            if (value is IMessageData { HasValue: true } messageData)
            {
                reference.Reference = messageData.Address;

                if (messageData is IInlineMessageData inlineMessageData)
                    inlineMessageData.Set(reference);
            }

            JsonSerializer.Serialize(writer, reference, options);
        }
    }


    sealed class MessageDataObjectConverter<T> :
        JsonConverter<MessageData<T>>
        where T : class
    {
        public override MessageData<T> Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            var converter = new SystemTextJsonObjectMessageDataConverter<T>(options);

            var reference = JsonSerializer.Deserialize<JsonMessageDataReference>(ref reader, options);
            if (reference?.Data != null)
                return new BytesInlineMessageData<T>(converter, reference.Data, reference.Reference);

            if (reference?.Reference == null)
                return EmptyMessageData<T>.Instance;

            return new DeserializedMessageData<T>(reference.Reference);
        }

        public override void Write(Utf8JsonWriter writer, MessageData<T> value, JsonSerializerOptions options)
        {
            var reference = new JsonMessageDataReference();

            if (value is IMessageData { HasValue: true } messageData)
            {
                reference.Reference = messageData.Address;

                if (messageData is IInlineMessageData inlineMessageData)
                    inlineMessageData.Set(reference);
            }

            JsonSerializer.Serialize(writer, reference, options);
        }
    }
}
