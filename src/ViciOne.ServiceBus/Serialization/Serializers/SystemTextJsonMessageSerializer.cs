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
    /// <summary>Identifies the media type used for envelope-encoded JSON messages.</summary>
    public const string JsonMediaType = "application/vnd.vicione.servicebus+json";

    /// <summary>Gets an independent content-type value for envelope-encoded JSON messages.</summary>
    public static ContentType JsonContentType => new(JsonMediaType);

    readonly string _contentType;
    readonly JsonSerializerOptions _options;

    /// <summary>Creates an envelope-encoded JSON serializer.</summary>
    /// <param name="options">The immutable JSON option snapshot.</param>
    /// <param name="contentType">An alternate envelope media type.</param>
    public SystemTextJsonMessageSerializer(JsonSerializerOptions options, ContentType? contentType = null)
    {
        _options = options ?? throw new ArgumentNullException(nameof(options));
        if (!_options.IsReadOnly)
            throw new ArgumentException("The serializer requires an immutable JsonSerializerOptions snapshot.", nameof(options));

        _contentType = (contentType ?? JsonContentType).ToString();
    }

    /// <summary>Gets the envelope media type.</summary>
    public ContentType ContentType => new(_contentType);

    /// <summary>Adds the JSON provider and media type to a diagnostic probe.</summary>
    /// <param name="context">The probe to populate.</param>
    public void Probe(ProbeContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        var scope = context.CreateScope("json");
        scope.Add("contentType", ContentType.MediaType);
        scope.Add("provider", "System.Text.Json");
    }

    /// <summary>Creates a consume context for an envelope-encoded transport delivery.</summary>
    /// <param name="receiveContext">The transport delivery to deserialize.</param>
    /// <returns>A consume context that materializes declared contracts lazily.</returns>
    public ConsumeContext Deserialize(ReceiveContext receiveContext)
    {
        ArgumentNullException.ThrowIfNull(receiveContext);
        return new BodyConsumeContext(receiveContext, Deserialize(receiveContext.Body, receiveContext.TransportHeaders, receiveContext.InputAddress));
    }

    /// <summary>Deserializes a JSON envelope and creates its serializer context.</summary>
    /// <param name="body">The encoded JSON envelope.</param>
    /// <param name="headers">The transport headers associated with the delivery.</param>
    /// <param name="destinationAddress">The endpoint that received the envelope.</param>
    /// <returns>The envelope metadata, declared contract set, and JSON message value.</returns>
    public SerializerContext Deserialize(MessageBody body, Headers headers, Uri? destinationAddress = null)
    {
        ArgumentNullException.ThrowIfNull(body);
        ArgumentNullException.ThrowIfNull(headers);

        try
        {
            JsonElement? bodyElement = body is IJsonMessageBody jsonMessageBody
                ? jsonMessageBody.GetJsonElement(_options)
                : JsonSerializer.Deserialize<JsonElement>(body.ToArray(), _options);

            var envelope = bodyElement?.Deserialize<MessageEnvelope>(_options);
            if (envelope == null)
                throw new SerializationException("Message envelope not found");

            var messageContext = new EnvelopeMessageContext(envelope, this);

            string[] messageTypes = envelope.MessageTypes is { } declaredTypes ? [.. declaredTypes] : [];

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

    /// <summary>Wraps existing JSON text as an encoded message body.</summary>
    /// <param name="text">The encoded JSON text.</param>
    /// <returns>A message body over the supplied text.</returns>
    public MessageBody GetMessageBody(string text)
    {
        ArgumentNullException.ThrowIfNull(text);
        return new StringMessageBody(text);
    }

    /// <summary>Creates an envelope-encoded JSON body for an outgoing message.</summary>
    /// <typeparam name="T">The outgoing message contract.</typeparam>
    /// <param name="context">The outgoing message and metadata.</param>
    /// <returns>An owned snapshot of the encoded envelope.</returns>
    public MessageBody GetMessageBody<T>(SendContext<T> context)
        where T : class
    {
        ArgumentNullException.ThrowIfNull(context);
        return new SystemTextJsonMessageBody<T>(context, _options);
    }

    /// <summary>Converts a native, text, or JSON metadata value to a reference type.</summary>
    /// <typeparam name="T">The requested reference type.</typeparam>
    /// <param name="value">The serialized or native value.</param>
    /// <param name="defaultValue">The value returned when the requested item is absent.</param>
    /// <returns>The converted value or <paramref name="defaultValue" />.</returns>
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

    /// <summary>Converts a native, text, or JSON metadata value to a value type.</summary>
    /// <typeparam name="T">The requested value type.</typeparam>
    /// <param name="value">The serialized or native value.</param>
    /// <param name="defaultValue">The value returned when the requested item is absent.</param>
    /// <returns>The converted value or <paramref name="defaultValue" />.</returns>
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

    /// <summary>Serializes an infrastructure metadata value as JSON.</summary>
    /// <param name="value">The value to serialize.</param>
    /// <returns>An empty body for <see langword="null" />; otherwise, a JSON body.</returns>
    public MessageBody SerializeObject(object? value)
    {
        if (value == null)
            return EmptyMessageBody.Instance;

        return new SystemTextJsonObjectMessageBody(value, _options);
    }
}
