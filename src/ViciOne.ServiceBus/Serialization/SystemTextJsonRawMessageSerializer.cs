using System;
using System.Net.Mime;
using System.Runtime.Serialization;
using System.Text.Json;

namespace ViciOne.ServiceBus.Serialization;

/// <summary>Serializes and deserializes raw JSON messages with <see cref="JsonSerializer" />.</summary>
public sealed class SystemTextJsonRawMessageSerializer :
    RawMessageSerializer,
    IMessageDeserializer,
    IMessageSerializer
{
    /// <summary>Gets the content type used for raw JSON messages.</summary>
    public static readonly ContentType JsonContentType = new ContentType("application/json");

    readonly IObjectDeserializer _objectDeserializer;
    readonly JsonSerializerOptions _serializerOptions;
    readonly RawSerializerOptions _rawOptions;

    /// <summary>Initializes a new instance.</summary>
    /// <param name="serializerOptions">The serializer options.</param>
    /// <param name="rawOptions">The raw options.</param>
    public SystemTextJsonRawMessageSerializer(JsonSerializerOptions serializerOptions, RawSerializerOptions rawOptions = RawSerializerOptions.Default)
    {
        _serializerOptions = serializerOptions ?? throw new ArgumentNullException(nameof(serializerOptions));
        if (!_serializerOptions.IsReadOnly)
            throw new ArgumentException("The serializer requires an immutable JsonSerializerOptions snapshot.", nameof(serializerOptions));

        _rawOptions = rawOptions;
        _objectDeserializer = new SystemTextJsonMessageSerializer(_serializerOptions);
    }

    /// <summary>Gets the content type.</summary>
    public ContentType ContentType => JsonContentType;

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
            JsonElement? bodyElement;
            if (body is IJsonMessageBody jsonMessageBody)
                bodyElement = jsonMessageBody.GetJsonElement(_serializerOptions);
            else
            {
                var bytes = body.GetBytes();
                bodyElement = bytes.Length > 0
                    ? JsonSerializer.Deserialize<JsonElement>(bytes, _serializerOptions)
                    : null;
            }

            bodyElement ??= JsonDocument.Parse("{}").RootElement;

            var messageTypes = headers.GetMessageTypes();
            var messageContext = new RawMessageContext(headers, destinationAddress, _rawOptions);

            return new SystemTextJsonRawSerializerContext(_objectDeserializer, _serializerOptions, ContentType, messageContext, messageTypes, _rawOptions,
                bodyElement.Value);
        }
        catch (SerializationException)
        {
            throw;
        }
        catch (Exception ex)
        {
            throw new SerializationException("An error occurred while deserializing the raw message envelope", ex);
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
        if (_rawOptions.HasFlag(RawSerializerOptions.AddTransportHeaders))
            SetRawMessageHeaders(context);

        return new SystemTextJsonRawMessageBody<T>(context, _serializerOptions);
    }
}
