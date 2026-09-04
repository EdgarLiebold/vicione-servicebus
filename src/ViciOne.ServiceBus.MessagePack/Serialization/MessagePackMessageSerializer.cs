using System;
using System.Collections.Generic;
using System.Net.Mime;
using MessagePack;
using ViciOne.ServiceBus.Initializers;
using ViciOne.ServiceBus.Initializers.TypeConverters;
using ViciOne.ServiceBus.Internals;

namespace ViciOne.ServiceBus.Serialization;

/// <summary>
/// Provides a message pack message serializer implementation.
/// </summary>
public class MessagePackMessageSerializer :
    IMessageSerializer,
    IMessageDeserializer,
    IObjectDeserializer
{
    const string ContentTypeHeaderValue = "application/vnd.vicione.servicebus+msgpack";
    const string ProviderKey = "MessagePack";

    /// <summary>
    /// Defines the message pack content type value.
    /// </summary>
    public static readonly ContentType MessagePackContentType = new(ContentTypeHeaderValue);

    /// <summary>
    /// Gets the content type value.
    /// </summary>
    public ContentType ContentType => MessagePackContentType;

    /// <summary>
    /// Performs the deserialize operation.
    /// </summary>
    /// <param name="receiveContext">The receive context value.</param>
    /// <returns>The result of the operation.</returns>
    public ConsumeContext Deserialize(ReceiveContext receiveContext)
    {
        var serializerContext = Deserialize(receiveContext.Body, receiveContext.TransportHeaders, receiveContext.InputAddress);
        return new BodyConsumeContext(receiveContext, serializerContext);
    }

    /// <summary>
    /// Performs the deserialize operation.
    /// </summary>
    /// <param name="body">The body value.</param>
    /// <param name="headers">The headers value.</param>
    /// <param name="destinationAddress">The destination address value.</param>
    /// <returns>The result of the operation.</returns>
    public SerializerContext Deserialize(MessageBody body, Headers headers, Uri? destinationAddress = null)
    {
        var messageBuffer = body.GetBytes();
        var envelope = DeserializeMessageBuffer<MessagePackEnvelope>(messageBuffer);

        var messageContext = new EnvelopeMessageContext(envelope, this);

        var messageTypes = envelope.MessageType ?? [];

        return new MessagePackMessageSerializerContext(this, messageContext, messageTypes, envelope);
    }

    /// <summary>
    /// Gets message body.
    /// </summary>
    /// <param name="text">The text value.</param>
    /// <returns>The result of the operation.</returns>
    public MessageBody GetMessageBody(string text)
    {
        return new Base64MessageBody(text);
    }

    /// <summary>
    /// Gets message body.
    /// </summary>
    /// <typeparam name="T">The t type.</typeparam>
    /// <param name="context">The operation context.</param>
    /// <returns>The result of the operation.</returns>
    public MessageBody GetMessageBody<T>(SendContext<T> context)
        where T : class
    {
        return new MessagePackMessageBody<T>(context);
    }

    /// <summary>
    /// Performs the probe operation.
    /// </summary>
    /// <param name="context">The operation context.</param>
    public void Probe(ProbeContext context)
    {
        var scope = context.CreateScope("messagepack");
        scope.Add("contentType", ContentType.MediaType);
        scope.Add("provider", ProviderKey);
    }

    /// <summary>
    /// Performs the ensure object buffer format is byte array operation.
    /// </summary>
    /// <param name="serializedObjectAsUnknownFormat">The serialized object as unknown format value.</param>
    /// <returns>The result of the operation.</returns>
    public static byte[] EnsureObjectBufferFormatIsByteArray(object serializedObjectAsUnknownFormat)
    {
        return serializedObjectAsUnknownFormat switch
        {
            string base64EncodedMessagePackBody => Convert.FromBase64String(base64EncodedMessagePackBody),
            byte[] messagePackBody => messagePackBody,
            _ => InternalMessagePackResolver.Serialize(serializedObjectAsUnknownFormat)
        };
    }

    /// <summary>
    /// Performs the deserialize object operation.
    /// </summary>
    /// <typeparam name="T">The t type.</typeparam>
    /// <param name="value">The value.</param>
    /// <param name="defaultValue">The default value value.</param>
    /// <returns>The result of the operation.</returns>
    public T? DeserializeObject<T>(object? value, T? defaultValue = default)
        where T : class
    {
        if (value is Dictionary<string, object> objectByStringPairs)
        {
            // If the object is a Dictionary<string, object>, we deserialize internally using JSON.
            // MessagePack is case-sensitive, and would not be able to deserialize without correct casing.

            return objectByStringPairs.Transform<T>(ServiceBusMetadataJson.Options);
        }

        return InternalDeserializeObject(value, defaultValue);
    }

    /// <summary>
    /// Performs the deserialize object operation.
    /// </summary>
    /// <typeparam name="T">The t type.</typeparam>
    /// <param name="value">The value.</param>
    /// <param name="defaultValue">The default value value.</param>
    /// <returns>The result of the operation.</returns>
    public T? DeserializeObject<T>(object? value, T? defaultValue = null)
        where T : struct
    {
        return InternalDeserializeObject(value, defaultValue);
    }

    /// <summary>
    /// Performs the serialize object operation.
    /// </summary>
    /// <param name="value">The value.</param>
    /// <returns>The result of the operation.</returns>
    public MessageBody SerializeObject(object? value)
    {
        if (value is null)
            return new EmptyMessageBody();

        return new MessagePackMessageBody<object>(value);
    }

    static T? InternalDeserializeObject<T>(object? value, T? defaultValue)
    {
        if (value is null || Equals(value, defaultValue))
            return defaultValue;

        if (value is T valueAsT)
            return valueAsT;

        if (value is string text
            && TypeConverterCache.TryGetTypeConverter<T, string>(out ITypeConverter<T, string>? typeConverter)
            && typeConverter.TryConvert(text, out var result))
            return result;

        var messageSerializedBuffer = EnsureObjectBufferFormatIsByteArray(value);

        return DeserializeMessageBuffer<T>(messageSerializedBuffer);
    }

    static T DeserializeMessageBuffer<T>(byte[] messageBuffer)
    {
        return InternalMessagePackResolver.Deserialize<T>(messageBuffer);
    }
}
