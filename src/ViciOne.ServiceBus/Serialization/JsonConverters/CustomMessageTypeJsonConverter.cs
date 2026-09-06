using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace ViciOne.ServiceBus.Serialization.JsonConverters;

/// <summary>Converts custom message type json values.</summary>
/// <typeparam name="T">The value type.</typeparam>
public class CustomMessageTypeJsonConverter<T> :
    JsonConverter<T>
    where T : class
{
    readonly JsonSerializerOptions _options;

    /// <summary>Initializes a new instance.</summary>
    /// <param name="options">The options that control the operation.</param>
    public CustomMessageTypeJsonConverter(JsonSerializerOptions options)
    {
        _options = options;
    }

    /// <summary>Reads the requested value.</summary>
    /// <param name="reader">The reader updated by the operation.</param>
    /// <param name="typeToConvert">The type to convert.</param>
    /// <param name="options">The options that control the operation.</param>
    /// <returns>The t produced by the operation.</returns>
    public override T? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        return JsonSerializer.Deserialize<T>(ref reader, _options);
    }

    /// <summary>Writes the supplied value.</summary>
    /// <param name="writer">The writer.</param>
    /// <param name="value">The value to process.</param>
    /// <param name="options">The options that control the operation.</param>
    public override void Write(Utf8JsonWriter writer, T value, JsonSerializerOptions options)
    {
        JsonSerializer.Serialize(writer, value, _options);
    }
}
