using System;
using System.Collections.Generic;
using System.Net.Mime;
using MessagePack;
using ViciOne.ServiceBus.Advanced;
using ViciOne.ServiceBus.Advanced.Serialization;
using ViciOne.ServiceBus.Initializers;
using ViciOne.ServiceBus.Initializers.TypeConverters;
using ViciOne.ServiceBus.Internals;
using ViciOne.ServiceBus.Operations;
using ViciOne.ServiceBus.Serialization;

namespace ViciOne.ServiceBus.MessagePack.Serialization;

/// <summary>Serializes ViciOne transport envelopes and standalone values with MessagePack.</summary>
internal sealed class MessagePackMessageSerializer :
    IMessageSerializer,
    IMessageDeserializer,
    IObjectDeserializer
{
    const string ProviderKey = "MessagePack";

    internal const string MediaType = "application/vnd.vicione.servicebus+msgpack";
    internal static ContentType MessagePackContentType => new(MediaType);

    /// <summary>Gets the media type produced and consumed by this serializer.</summary>
    public ContentType ContentType => new(MediaType);

    /// <summary>Deserializes the received envelope and combines its serializer context with the transport context.</summary>
    /// <param name="receiveContext">The receive context that supplies the body and transport metadata.</param>
    /// <returns>A consume context backed by the decoded MessagePack envelope.</returns>
    public ConsumeContext Deserialize(ReceiveContext receiveContext)
    {
        ArgumentNullException.ThrowIfNull(receiveContext);
        var serializerContext = Deserialize(receiveContext.Body, receiveContext.TransportHeaders, receiveContext.InputAddress);
        return new BodyConsumeContext(receiveContext, serializerContext);
    }

    /// <summary>Deserializes a MessagePack transport envelope into a serializer context.</summary>
    /// <param name="body">The body containing the encoded envelope.</param>
    /// <param name="headers">The transport headers associated with the body.</param>
    /// <param name="destinationAddress">The transport destination associated with the body.</param>
    /// <returns>A serializer context that exposes the envelope metadata and lazily decodes message contracts.</returns>
    public SerializerContext Deserialize(MessageBody body, Headers headers, Uri? destinationAddress = null)
    {
        ArgumentNullException.ThrowIfNull(body);
        ArgumentNullException.ThrowIfNull(headers);
        var messageBuffer = body.GetBytes();
        var envelope = DeserializeMessageBuffer<MessagePackEnvelope>(messageBuffer);

        var messageContext = new EnvelopeMessageContext(envelope, this);

        var messageTypes = envelope.MessageType ?? [];

        return new MessagePackMessageSerializerContext(this, messageContext, messageTypes, envelope);
    }

    /// <summary>Creates a message body from Base64-encoded MessagePack text.</summary>
    /// <param name="text">The Base64-encoded MessagePack bytes.</param>
    /// <returns>A body that decodes the supplied Base64 text on access.</returns>
    public MessageBody GetMessageBody(string text)
    {
        ArgumentNullException.ThrowIfNull(text);
        return new Base64MessageBody(text);
    }

    /// <summary>Creates a lazily serialized MessagePack envelope for a send context.</summary>
    /// <typeparam name="T">The message contract being sent.</typeparam>
    /// <param name="context">The send context that supplies message content and envelope metadata.</param>
    /// <returns>A lazy MessagePack message body.</returns>
    public MessageBody GetMessageBody<T>(SendContext<T> context)
        where T : class
    {
        ArgumentNullException.ThrowIfNull(context);
        return new MessagePackMessageBody<T>(context);
    }

    /// <summary>Adds this serializer's media type and provider identity to a probe.</summary>
    /// <param name="context">The probe context that receives the serializer details.</param>
    public void Probe(ProbeContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        var scope = context.CreateScope("messagepack");
        scope.Add("contentType", ContentType.MediaType);
        scope.Add("provider", ProviderKey);
    }

    /// <summary>Normalizes Base64 text, MessagePack bytes, or an arbitrary value to MessagePack bytes.</summary>
    /// <param name="serializedObjectAsUnknownFormat">The Base64 text, byte array, or value to normalize.</param>
    /// <returns>The original byte array, decoded Base64 bytes, or newly serialized MessagePack bytes.</returns>
    public static byte[] EnsureObjectBufferFormatIsByteArray(object serializedObjectAsUnknownFormat)
    {
        ArgumentNullException.ThrowIfNull(serializedObjectAsUnknownFormat);
        return serializedObjectAsUnknownFormat switch
        {
            string base64EncodedMessagePackBody => Convert.FromBase64String(base64EncodedMessagePackBody),
            byte[] messagePackBody => messagePackBody,
            _ => InternalMessagePackResolver.Serialize(serializedObjectAsUnknownFormat)
        };
    }

    /// <summary>Converts an envelope value to a reference-type contract.</summary>
    /// <typeparam name="T">The requested reference-type contract.</typeparam>
    /// <param name="value">A direct value, object dictionary, textual scalar, Base64 body, or MessagePack payload.</param>
    /// <param name="defaultValue">The value returned when <paramref name="value"/> is absent or equal to this default.</param>
    /// <returns>The existing instance, converted scalar, projected dictionary, decoded contract, or supplied default.</returns>
    public T? DeserializeObject<T>(object? value, T? defaultValue = default)
        where T : class
    {
        if (value is Dictionary<string, object> objectByStringPairs)
        {
            return objectByStringPairs.Transform<T>(ServiceBusMetadataJson.Options);
        }

        return InternalDeserializeObject(value, defaultValue);
    }

    /// <summary>Converts an envelope value to a nullable value-type contract.</summary>
    /// <typeparam name="T">The requested value type.</typeparam>
    /// <param name="value">A direct value, textual scalar, Base64 body, or MessagePack payload.</param>
    /// <param name="defaultValue">The value returned when <paramref name="value"/> is absent or equal to this default.</param>
    /// <returns>The direct value, converted scalar, decoded value, or supplied default.</returns>
    public T? DeserializeObject<T>(object? value, T? defaultValue = null)
        where T : struct
    {
        return InternalDeserializeObject(value, defaultValue);
    }

    /// <summary>Creates a standalone MessagePack body for an object.</summary>
    /// <param name="value">The value to serialize, or <see langword="null"/> for an empty body.</param>
    /// <returns>A lazy MessagePack body, or an empty body for a null value.</returns>
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
