using System.Collections.Generic;
using System.Linq;
using System.Net.Mime;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace ViciOne.ServiceBus.Serialization;

/// <summary>Serializes a previously deserialized message for forwarding or deferred delivery.</summary>
internal sealed class SystemTextJsonForwardingSerializer :
    RawMessageSerializer,
    IMessageSerializer
{
    readonly JsonMessageEnvelope? _envelope;
    readonly string _contentType;
    readonly string[]? _messageTypes;
    readonly JsonSerializerOptions _options;
    readonly RawSerializerOptions? _rawOptions;
    object? _message;

    /// <summary>Creates a serializer that preserves an existing envelope.</summary>
    /// <param name="envelope">The envelope metadata and message.</param>
    /// <param name="contentType">The envelope media type.</param>
    /// <param name="options">The immutable JSON serializer options.</param>
    /// <param name="messageTypes">Optional replacement message contract URNs.</param>
    public SystemTextJsonForwardingSerializer(MessageEnvelope envelope, ContentType contentType, JsonSerializerOptions options,
        string[]? messageTypes = null)
    {
        ArgumentNullException.ThrowIfNull(envelope);
        ArgumentNullException.ThrowIfNull(contentType);
        ArgumentNullException.ThrowIfNull(options);

        _message = envelope.Message;
        _options = options;
        _messageTypes = messageTypes?.ToArray();

        _envelope = new JsonMessageEnvelope(envelope);

        _contentType = contentType.ToString();
    }

    /// <summary>Creates a serializer for a raw replacement message.</summary>
    /// <param name="message">The replacement message or source envelope.</param>
    /// <param name="contentType">The raw message media type.</param>
    /// <param name="options">The immutable JSON serializer options.</param>
    /// <param name="rawOptions">The raw header and message-type policy.</param>
    /// <param name="messageTypes">Optional replacement message contract URNs.</param>
    public SystemTextJsonForwardingSerializer(object message, ContentType contentType, JsonSerializerOptions options, RawSerializerOptions rawOptions,
        string[]? messageTypes = null)
    {
        ArgumentNullException.ThrowIfNull(message);
        ArgumentNullException.ThrowIfNull(contentType);
        ArgumentNullException.ThrowIfNull(options);
        if ((rawOptions & ~RawSerializerOptions.All) != 0)
            throw new ArgumentOutOfRangeException(nameof(rawOptions), rawOptions, "The raw serializer options contain unsupported flags.");

        if (message is MessageEnvelope envelope)
        {
            _message = envelope.Message;
            _envelope = new JsonMessageEnvelope(envelope);
        }
        else
            _message = message;

        _options = options;
        _rawOptions = rawOptions;
        _messageTypes = messageTypes?.ToArray();

        _contentType = contentType.ToString();
    }

    /// <summary>Gets the media type produced by this serializer.</summary>
    public ContentType ContentType => new(_contentType);

    /// <summary>Applies preserved contract metadata and creates the encoded outgoing body.</summary>
    /// <typeparam name="T">The outgoing message contract.</typeparam>
    /// <param name="context">The outgoing message and metadata.</param>
    /// <returns>An envelope-encoded or raw JSON body, according to this serializer's mode.</returns>
    public MessageBody GetMessageBody<T>(SendContext<T> context)
        where T : class
    {
        ArgumentNullException.ThrowIfNull(context);
        _envelope?.Update(context);

        if (_messageTypes != null)
            context.SupportedMessageTypes = _messageTypes;

        if (_rawOptions.HasValue)
        {
            if (_rawOptions.Value.HasFlag(RawSerializerOptions.AddTransportHeaders))
                SetRawMessageHeaders(context);

            return new SystemTextJsonRawMessageBody<T>(context, _options, _message);
        }

        return new SystemTextJsonMessageBody<T>(context, _options, _envelope);
    }

    /// <summary>Merges non-null replacement properties into a JSON object or appends replacement array elements.</summary>
    /// <param name="message">The replacement message values.</param>
    public void Overlay(object message)
    {
        ArgumentNullException.ThrowIfNull(message);
        if (_message is JsonElement element)
        {
            var overlayElement = JsonSerializer.SerializeToElement(message, _options);
            if ((element.ValueKind == JsonValueKind.Object || element.ValueKind == JsonValueKind.Array) && element.ValueKind == overlayElement.ValueKind)
                _message = Merge(element, overlayElement);
            else
                _message = message;
        }
        else
            _message = message;

        if (_envelope != null)
            _envelope.Message = _message;
    }

    static JsonNode Merge(JsonElement original, JsonElement overlay)
    {
        return original.ValueKind == JsonValueKind.Array
            ? MergeArray(original, overlay)
            : MergeObject(original, overlay);
    }

    static JsonNode MergeObject(JsonElement original, JsonElement overlay)
    {
        var jsonObject = new JsonObject();

        foreach (var property in original.EnumerateObject())
        {
            var name = property.Name;

            if (overlay.TryGetProperty(name, out var overlayElement) && overlayElement.ValueKind != JsonValueKind.Null)
            {
                jsonObject.Add(name, property.Value.ValueKind switch
                {
                    JsonValueKind.Object when overlayElement.ValueKind == JsonValueKind.Object => MergeObject(property.Value, overlayElement),
                    JsonValueKind.Array when overlayElement.ValueKind == JsonValueKind.Array => MergeArray(property.Value, overlayElement),
                    _ => ToNode(overlayElement)
                });
            }
            else
                jsonObject.Add(name, ToNode(property.Value));
        }

        foreach (var property in overlay.EnumerateObject().Where(property => !original.TryGetProperty(property.Name, out _)))
            jsonObject.Add(property.Name, ToNode(property.Value));

        return jsonObject;
    }

    static JsonNode MergeArray(JsonElement original, JsonElement overlay)
    {
        var jsonArray = new JsonArray();

        foreach (var element in original.EnumerateArray())
            jsonArray.Add(ToNode(element));

        foreach (var element in overlay.EnumerateArray())
            jsonArray.Add(ToNode(element));

        return jsonArray;
    }

    static JsonNode? ToNode(JsonElement element)
    {
        return element.ValueKind switch
        {
            JsonValueKind.Null => null,
            JsonValueKind.Array => new JsonArray(element.EnumerateArray().Select(x => ToNode(x)).ToArray()),
            JsonValueKind.Object => new JsonObject(element.EnumerateObject().Select(x => new KeyValuePair<string, JsonNode?>(x.Name, ToNode(x.Value)))),
            _ => JsonNode.Parse(element.GetRawText())
        };
    }
}
