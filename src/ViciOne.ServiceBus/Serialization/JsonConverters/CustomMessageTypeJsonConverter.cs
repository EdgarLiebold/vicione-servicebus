using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace ViciOne.ServiceBus.Serialization.JsonConverters;

/// <summary>
/// Provides a custom message type json converter implementation.
/// </summary>
/// <typeparam name="T">The t type.</typeparam>
public class CustomMessageTypeJsonConverter<T> :
    JsonConverter<T>
    where T : class
{
    readonly JsonSerializerOptions _options;

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="options">The options value.</param>
    public CustomMessageTypeJsonConverter(JsonSerializerOptions options)
    {
        _options = options;
    }

    /// <summary>
    /// Performs the read operation.
    /// </summary>
    /// <param name="reader">The reader value.</param>
    /// <param name="typeToConvert">The type to convert value.</param>
    /// <param name="options">The options value.</param>
    /// <returns>The result of the operation.</returns>
    public override T? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        return JsonSerializer.Deserialize<T>(ref reader, _options);
    }

    /// <summary>
    /// Performs the write operation.
    /// </summary>
    /// <param name="writer">The writer value.</param>
    /// <param name="value">The value.</param>
    /// <param name="options">The options value.</param>
    public override void Write(Utf8JsonWriter writer, T value, JsonSerializerOptions options)
    {
        JsonSerializer.Serialize(writer, value, _options);
    }
}
