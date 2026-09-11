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
    /// <summary>Identifies the media type used for raw JSON messages.</summary>
    public const string JsonMediaType = "application/json";

    /// <summary>Gets an independent content-type value for raw JSON messages.</summary>
    public static ContentType JsonContentType => new(JsonMediaType);

    readonly IObjectDeserializer _objectDeserializer;
    readonly JsonSerializerOptions _serializerOptions;
    readonly RawSerializerOptions _rawOptions;

    /// <summary>Creates a raw JSON serializer.</summary>
    /// <param name="serializerOptions">The immutable JSON option snapshot.</param>
    /// <param name="rawOptions">The message-type and header policy.</param>
    public SystemTextJsonRawMessageSerializer(JsonSerializerOptions serializerOptions, RawSerializerOptions rawOptions = RawSerializerOptions.Default)
    {
        _serializerOptions = serializerOptions ?? throw new ArgumentNullException(nameof(serializerOptions));
        if (!_serializerOptions.IsReadOnly)
            throw new ArgumentException("The serializer requires an immutable JsonSerializerOptions snapshot.", nameof(serializerOptions));
        if ((rawOptions & ~RawSerializerOptions.All) != 0)
            throw new ArgumentOutOfRangeException(nameof(rawOptions), rawOptions, "The raw serializer options contain unsupported flags.");

        _rawOptions = rawOptions;
        _objectDeserializer = new SystemTextJsonMessageSerializer(_serializerOptions);
    }

    /// <summary>Gets the standard JSON media type.</summary>
    public ContentType ContentType => new(JsonMediaType);

    /// <summary>Adds the JSON provider and media type to a diagnostic probe.</summary>
    /// <param name="context">The probe to populate.</param>
    public void Probe(ProbeContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        var scope = context.CreateScope("json");
        scope.Add("contentType", ContentType.MediaType);
        scope.Add("provider", "System.Text.Json");
    }

    /// <summary>Creates a consume context for a raw JSON transport delivery.</summary>
    /// <param name="receiveContext">The transport delivery to deserialize.</param>
    /// <returns>A consume context that materializes accepted contracts lazily.</returns>
    public ConsumeContext Deserialize(ReceiveContext receiveContext)
    {
        ArgumentNullException.ThrowIfNull(receiveContext);
        return new BodyConsumeContext(receiveContext, Deserialize(receiveContext.Body, receiveContext.TransportHeaders, receiveContext.InputAddress));
    }

    /// <summary>Deserializes a raw JSON body and projects its metadata from transport headers.</summary>
    /// <param name="body">The encoded raw JSON value.</param>
    /// <param name="headers">The transport headers that carry metadata and contract URNs.</param>
    /// <param name="destinationAddress">The endpoint that received the message.</param>
    /// <returns>The projected metadata, accepted contract set, and raw JSON value.</returns>
    public SerializerContext Deserialize(MessageBody body, Headers headers, Uri? destinationAddress = null)
    {
        ArgumentNullException.ThrowIfNull(body);
        ArgumentNullException.ThrowIfNull(headers);

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

            bodyElement ??= JsonSerializer.SerializeToElement(new object(), _serializerOptions);

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

    /// <summary>Wraps existing JSON text as an encoded message body.</summary>
    /// <param name="text">The encoded JSON text.</param>
    /// <returns>A message body over the supplied text.</returns>
    public MessageBody GetMessageBody(string text)
    {
        ArgumentNullException.ThrowIfNull(text);
        return new StringMessageBody(text);
    }

    /// <summary>Creates a raw JSON body for an outgoing message and optionally emits transport headers.</summary>
    /// <typeparam name="T">The outgoing message contract.</typeparam>
    /// <param name="context">The outgoing message and metadata.</param>
    /// <returns>A lazily encoded raw JSON body.</returns>
    public MessageBody GetMessageBody<T>(SendContext<T> context)
        where T : class
    {
        ArgumentNullException.ThrowIfNull(context);
        if (_rawOptions.HasFlag(RawSerializerOptions.AddTransportHeaders))
            SetRawMessageHeaders(context);

        return new SystemTextJsonRawMessageBody<T>(context, _serializerOptions);
    }
}
