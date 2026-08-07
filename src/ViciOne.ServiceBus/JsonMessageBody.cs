// ViciOne modification: WP-F2-SERVICEBUS-IDENTITY, 2026-08-07.
namespace ViciOne.ServiceBus;

using System.Text.Json;


/// <summary>
/// If the incoming message is in a JSON format, use this to unwrap the JSON document from any transport-specific encapsulation
/// </summary>
public interface JsonMessageBody
{
    JsonElement? GetJsonElement(JsonSerializerOptions options);
}
