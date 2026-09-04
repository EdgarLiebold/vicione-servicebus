using System;
using System.Net.Mime;
using System.Runtime.Serialization;
using System.Text.Json;

#nullable enable
namespace ViciOne.ServiceBus.Serialization;

public class SystemTextJsonRawMessageSerializer :
    RawMessageSerializer,
    IMessageDeserializer,
    IMessageSerializer
{
    public static readonly ContentType JsonContentType = new ContentType("application/json");

    readonly IObjectDeserializer _objectDeserializer;
    readonly JsonSerializerOptions _serializerOptions;
    readonly RawSerializerOptions _rawOptions;

    public SystemTextJsonRawMessageSerializer(JsonSerializerOptions serializerOptions, RawSerializerOptions rawOptions = RawSerializerOptions.Default)
    {
        _serializerOptions = serializerOptions ?? throw new ArgumentNullException(nameof(serializerOptions));
        if (!_serializerOptions.IsReadOnly)
            throw new ArgumentException("The serializer requires an immutable JsonSerializerOptions snapshot.", nameof(serializerOptions));

        _rawOptions = rawOptions;
        _objectDeserializer = new SystemTextJsonMessageSerializer(_serializerOptions);
    }

    public ContentType ContentType => JsonContentType;

    public void Probe(ProbeContext context)
    {
        var scope = context.CreateScope("json");
        scope.Add("contentType", ContentType.MediaType);
        scope.Add("provider", "System.Text.Json");
    }

    public ConsumeContext Deserialize(ReceiveContext receiveContext)
    {
        return new BodyConsumeContext(receiveContext, Deserialize(receiveContext.Body, receiveContext.TransportHeaders, receiveContext.InputAddress));
    }

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

    public MessageBody GetMessageBody(string text)
    {
        return new StringMessageBody(text);
    }

    public MessageBody GetMessageBody<T>(SendContext<T> context)
        where T : class
    {
        if (_rawOptions.HasFlag(RawSerializerOptions.AddTransportHeaders))
            SetRawMessageHeaders(context);

        return new SystemTextJsonRawMessageBody<T>(context, _serializerOptions);
    }
}
