using System.Text.Json;

namespace ViciOne.ServiceBus;
/// <summary>
/// If the incoming message is in a JSON format, use this to unwrap the JSON document from any transport-specific encapsulation
/// </summary>
public interface JsonMessageBody
{
    JsonElement? GetJsonElement(JsonSerializerOptions options);
}
