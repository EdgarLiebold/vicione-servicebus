using System.Text.Json;

namespace ViciOne.ServiceBus.Advanced;
/// <summary>
/// If the incoming message is in a JSON format, use this to unwrap the JSON document from any transport-specific encapsulation
/// </summary>
public interface JsonMessageBody
{
    /// <summary>
    /// Gets json element.
    /// </summary>
    /// <param name="options">The options value.</param>
    /// <returns>The result of the operation.</returns>
    JsonElement? GetJsonElement(JsonSerializerOptions options);
}
