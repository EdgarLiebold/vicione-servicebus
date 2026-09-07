using System.Text.Json;

namespace ViciOne.ServiceBus.Advanced;

/// <summary>Exposes a JSON message body independently of its transport representation.</summary>
public interface IJsonMessageBody
{
    /// <summary>Deserializes the body as a JSON element.</summary>
    /// <param name="options">The JSON serializer options.</param>
    /// <returns>The JSON value, or <see langword="null"/> when the body contains no JSON value.</returns>
    JsonElement? GetJsonElement(JsonSerializerOptions options);
}
