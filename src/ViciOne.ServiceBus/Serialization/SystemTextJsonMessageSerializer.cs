using System;
using System.Net.Mime;
using System.Runtime.Serialization;
using System.Text.Json;
using ViciOne.ServiceBus.Initializers;
using ViciOne.ServiceBus.Initializers.TypeConverters;

namespace ViciOne.ServiceBus.Serialization;

/// <summary>Serializes and deserializes envelope-encoded messages with <see cref="JsonSerializer" />.</summary>
public sealed class SystemTextJsonMessageSerializer :
    IMessageDeserializer,
    IMessageSerializer,
    IObjectDeserializer
{
    /// <summary>Gets the content type used for envelope-encoded JSON messages.</summary>
    public static readonly ContentType JsonContentType = new ContentType("application/vnd.vicione.servicebus+json");

    readonly JsonSerializerOptions _options;

    /// <summary>Initializes a new instance.</summary>
    /// <param name="options">The options that control the operation.</param>
    /// <param name="contentType">The runtime content type used by the operation.</param>
    public SystemTextJsonMessageSerializer(JsonSerializerOptions options, ContentType? contentType = null)
    {
        _options = options ?? throw new ArgumentNullException(nameof(options));
        if (!_options.IsReadOnly)
            throw new ArgumentException("The serializer requires an immutable JsonSerializerOptions snapshot.", nameof(options));

        ContentType = contentType ?? JsonContentType;
    }

    /// <summary>Gets the content type.</summary>
    public ContentType ContentType { get; }

    /// <summary>Writes diagnostic information to the probe context.</summary>
    /// <param name="context">The context associated with the operation.</param>
    public void Probe(ProbeContext context)
    {
        var scope = context.CreateScope("json");
        scope.Add("contentType", ContentType.MediaType);
        scope.Add("provider", "System.Text.Json");
    }

    /// <summary>Deserializes the supplied payload.</summary>
    /// <param name="receiveContext">The receive context.</param>
    /// <returns>The deserialized value.</returns>
    public ConsumeContext Deserialize(ReceiveContext receiveContext)
    {
        return new BodyConsumeContext(receiveContext, Deserialize(receiveContext.Body, receiveContext.TransportHeaders, receiveContext.InputAddress));
    }

    /// <summary>Deserializes the supplied payload.</summary>
    /// <param name="body">The body.</param>
    /// <param name="headers">The headers.</param>
    /// <param name="destinationAddress">The destination address.</param>
    /// <returns>The deserialized value.</returns>
    public SerializerContext Deserialize(MessageBody body, Headers headers, Uri? destinationAddress = null)
    {
        try
        {
            JsonElement? bodyElement = body is IJsonMessageBody jsonMessageBody
                ? jsonMessageBody.GetJsonElement(_options)
                : JsonSerializer.Deserialize<JsonElement>(body.GetBytes(), _options);

            var envelope = bodyElement?.Deserialize<MessageEnvelope>(_options);
            if (envelope == null)
                throw new SerializationException("Message envelope not found");

            var messageContext = new EnvelopeMessageContext(envelope, this);

            var messageTypes = envelope.MessageType ?? [];

            return new SystemTextJsonSerializerContext(this, _options, ContentType, messageContext, messageTypes, envelope);
        }
        catch (SerializationException)
        {
            throw;
        }
        catch (Exception ex)
        {
            throw new SerializationException("An error occurred while deserializing the message envelope", ex);
        }
    }

    /// <summary>Gets message body.</summary>
    /// <param name="text">The text.</param>
    /// <returns>The message body.</returns>
    public MessageBody GetMessageBody(string text)
    {
        return new StringMessageBody(text);
    }

    /// <summary>Gets message body.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="context">The context associated with the operation.</param>
    /// <returns>The message body.</returns>
    public MessageBody GetMessageBody<T>(SendContext<T> context)
        where T : class
    {
        return new SystemTextJsonMessageBody<T>(context, _options);
    }

    /// <summary>Deserializes object.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="value">The value to process.</param>
    /// <param name="defaultValue">The value returned when the requested item is absent.</param>
    /// <returns>The deserialized object.</returns>
    public T? DeserializeObject<T>(object? value, T? defaultValue = default)
        where T : class
    {
        switch (value)
        {
            case null:
                return defaultValue;
            case T returnValue:
                return returnValue;
            case string text when string.IsNullOrWhiteSpace(text):
                return defaultValue;
            case string text when TypeConverterCache.TryGetTypeConverter(out ITypeConverter<T, string>? typeConverter)
                && typeConverter.TryConvert(text, out var result):
                return result;
            case string text:
                return JsonSerializer.Deserialize<JsonElement>(text, _options).GetObject<T>(_options);
            case JsonElement jsonElement:
                return jsonElement.GetObject<T>(_options);
        }

        var element = JsonSerializer.SerializeToElement(value, _options);

        return element.ValueKind == JsonValueKind.Null
            ? defaultValue
            : element.GetObject<T>(_options);
    }

    /// <summary>Deserializes object.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="value">The value to process.</param>
    /// <param name="defaultValue">The value returned when the requested item is absent.</param>
    /// <returns>The deserialized object.</returns>
    public T? DeserializeObject<T>(object? value, T? defaultValue = null)
        where T : struct
    {
        switch (value)
        {
            case null:
                return defaultValue;
            case T returnValue:
                return returnValue;
            case string text when string.IsNullOrWhiteSpace(text):
                return defaultValue;
            case string text when TypeConverterCache.TryGetTypeConverter(out ITypeConverter<T, string>? typeConverter)
                && typeConverter.TryConvert(text, out var result):
                return result;
            case string text:
                return JsonSerializer.Deserialize<T>(text, _options);
            case JsonElement jsonElement:
                return jsonElement.Deserialize<T>(_options);
        }

        var element = JsonSerializer.SerializeToElement(value, _options);

        return element.ValueKind == JsonValueKind.Null
            ? defaultValue
            : element.Deserialize<T>(_options);
    }

    /// <summary>Serializes object.</summary>
    /// <param name="value">The value to process.</param>
    /// <returns>The serialized object.</returns>
    public MessageBody SerializeObject(object? value)
    {
        if (value == null)
            return new EmptyMessageBody();

        return new SystemTextJsonObjectMessageBody(value, _options);
    }
}
