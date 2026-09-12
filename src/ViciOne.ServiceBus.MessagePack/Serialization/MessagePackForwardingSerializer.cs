using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Mime;
using System.Text.Json;
using System.Text.Json.Nodes;
using MessagePack;
using ViciOne.ServiceBus.Advanced;
using ViciOne.ServiceBus.Advanced.Serialization;
using ViciOne.ServiceBus.Serialization;

namespace ViciOne.ServiceBus.MessagePack.Serialization;

/// <summary>Forwards a private MessagePack envelope snapshot while applying current send metadata.</summary>
internal sealed class MessagePackForwardingSerializer :
    IMessageSerializer
{
    /// <summary>Gets the MessagePack envelope media type.</summary>
    public ContentType ContentType { get; } = MessagePackMessageSerializer.MessagePackContentType;

    readonly MessagePackEnvelope _envelope;

    /// <summary>Creates a forwarding serializer with an isolated copy of the supplied envelope.</summary>
    /// <param name="envelope">The envelope whose payload and metadata are preserved.</param>
    public MessagePackForwardingSerializer(MessageEnvelope envelope)
    {
        ArgumentNullException.ThrowIfNull(envelope);
        _envelope = new MessagePackEnvelope(envelope);
    }

    /// <summary>Creates an owned body from a private envelope snapshot and the current send metadata.</summary>
    /// <typeparam name="T">The forwarded message contract.</typeparam>
    /// <param name="context">The send context whose metadata is applied to the snapshot.</param>
    /// <returns>An eagerly serialized MessagePack transport body independent of later calls.</returns>
    public MessageBody GetMessageBody<T>(SendContext<T> context)
        where T : class
    {
        ArgumentNullException.ThrowIfNull(context);
        var envelope = new MessagePackEnvelope(_envelope);
        envelope.Update(context);

        if (envelope.MessageTypes != null)
            context.SupportedMessageTypes = envelope.MessageTypes;

        return new MessagePackMessageBody<T>(context, envelope);
    }

    /// <summary>
    /// Overlays replacement values by case-insensitive property name, recursively merges objects,
    /// appends arrays, and preserves an existing value when its replacement is null.
    /// </summary>
    /// <typeparam name="T">The replacement contract.</typeparam>
    /// <param name="message">The replacement values to apply.</param>
    public void Overlay<T>(T message)
        where T : class
    {
        ArgumentNullException.ThrowIfNull(message);
        Dictionary<string, object> currentMessage;

        if (_envelope.Message is not null)
        {
            var payload = MessagePackMessageSerializer.GetSerializedPayloadBytes(_envelope.Message);
            currentMessage = MessagePackSerializationRuntime
                .Deserialize<Dictionary<string, object>>(payload);
        }
        else
            currentMessage = [];

        JsonElement original = JsonSerializer.SerializeToElement(currentMessage, ServiceBusMetadataJson.Options);
        JsonElement overlay = JsonSerializer.SerializeToElement(message, ServiceBusMetadataJson.Options);
        JsonNode merged = MergeObject(original, overlay);
        currentMessage = merged.Deserialize<Dictionary<string, object>>(ServiceBusMetadataJson.Options)
            ?? new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);

        _envelope.IsNativeMessagePackPayload = false;
        _envelope.Message = MessagePackSerializationRuntime.Serialize(currentMessage);
    }

    static JsonObject MergeObject(JsonElement original, JsonElement overlay)
    {
        var result = new JsonObject();

        foreach (JsonProperty property in original.EnumerateObject())
        {
            if (TryGetProperty(overlay, property.Name, out JsonElement overlayValue) &&
                overlayValue.ValueKind != JsonValueKind.Null)
            {
                result.Add(property.Name, property.Value.ValueKind switch
                {
                    JsonValueKind.Object when overlayValue.ValueKind == JsonValueKind.Object =>
                        MergeObject(property.Value, overlayValue),
                    JsonValueKind.Array when overlayValue.ValueKind == JsonValueKind.Array =>
                        MergeArray(property.Value, overlayValue),
                    _ => ToNode(overlayValue),
                });
            }
            else
                result.Add(property.Name, ToNode(property.Value));
        }

        foreach (JsonProperty property in overlay.EnumerateObject()
                     .Where(property => !TryGetProperty(original, property.Name, out _)))
            result.Add(property.Name, ToNode(property.Value));

        return result;
    }

    static JsonArray MergeArray(JsonElement original, JsonElement overlay)
    {
        var result = new JsonArray();

        foreach (JsonElement element in original.EnumerateArray())
            result.Add(ToNode(element));

        foreach (JsonElement element in overlay.EnumerateArray())
            result.Add(ToNode(element));

        return result;
    }

    static bool TryGetProperty(JsonElement value, string propertyName, out JsonElement propertyValue)
    {
        foreach (JsonProperty property in value.EnumerateObject())
        {
            if (string.Equals(property.Name, propertyName, StringComparison.OrdinalIgnoreCase))
            {
                propertyValue = property.Value;
                return true;
            }
        }

        propertyValue = default;
        return false;
    }

    static JsonNode? ToNode(JsonElement value)
    {
        return value.ValueKind switch
        {
            JsonValueKind.Null => null,
            JsonValueKind.Array => new JsonArray([.. value.EnumerateArray().Select(ToNode)]),
            JsonValueKind.Object => new JsonObject(value.EnumerateObject()
                .Select(property => new KeyValuePair<string, JsonNode?>(property.Name, ToNode(property.Value)))),
            _ => JsonNode.Parse(value.GetRawText()),
        };
    }
}
