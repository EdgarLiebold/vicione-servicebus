using System;
using System.Net.Mime;
using System.Runtime.Serialization;
using System.Text.Json;

namespace ViciOne.ServiceBus.Serialization;

/// <summary>
/// Provides a system text json raw message serializer implementation.
/// </summary>
public class SystemTextJsonRawMessageSerializer :
    RawMessageSerializer,
    IMessageDeserializer,
    IMessageSerializer
{
    /// <summary>
    /// Defines the json content type value.
    /// </summary>
    public static readonly ContentType JsonContentType = new ContentType("application/json");

    readonly IObjectDeserializer _objectDeserializer;
    readonly JsonSerializerOptions _serializerOptions;
    readonly RawSerializerOptions _rawOptions;

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="serializerOptions">The serializer options value.</param>
    /// <param name="rawOptions">The raw options value.</param>
    public SystemTextJsonRawMessageSerializer(JsonSerializerOptions serializerOptions, RawSerializerOptions rawOptions = RawSerializerOptions.Default)
    {
        _serializerOptions = serializerOptions ?? throw new ArgumentNullException(nameof(serializerOptions));
        if (!_serializerOptions.IsReadOnly)
            throw new ArgumentException("The serializer requires an immutable JsonSerializerOptions snapshot.", nameof(serializerOptions));

        _rawOptions = rawOptions;
        _objectDeserializer = new SystemTextJsonMessageSerializer(_serializerOptions);
    }

    /// <summary>
    /// Gets the content type value.
    /// </summary>
    public ContentType ContentType => JsonContentType;

    /// <summary>
    /// Performs the probe operation.
    /// </summary>
    /// <param name="context">The operation context.</param>
    public void Probe(ProbeContext context)
    {
        var scope = context.CreateScope("json");
        scope.Add("contentType", ContentType.MediaType);
        scope.Add("provider", "System.Text.Json");
    }

    /// <summary>
    /// Performs the deserialize operation.
    /// </summary>
    /// <param name="receiveContext">The receive context value.</param>
    /// <returns>The result of the operation.</returns>
    public ConsumeContext Deserialize(ReceiveContext receiveContext)
    {
        return new BodyConsumeContext(receiveContext, Deserialize(receiveContext.Body, receiveContext.TransportHeaders, receiveContext.InputAddress));
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
        try
        {
            JsonElement? bodyElement;
            if (body is JsonMessageBody jsonMessageBody)
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

    /// <summary>
    /// Gets message body.
    /// </summary>
    /// <param name="text">The text value.</param>
    /// <returns>The result of the operation.</returns>
    public MessageBody GetMessageBody(string text)
    {
        return new StringMessageBody(text);
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
        if (_rawOptions.HasFlag(RawSerializerOptions.AddTransportHeaders))
            SetRawMessageHeaders(context);

        return new SystemTextJsonRawMessageBody<T>(context, _serializerOptions);
    }
}
