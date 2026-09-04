using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace ViciOne.ServiceBus.Serialization.JsonConverters;

/// <summary>
/// Provides an uri dictionary system text json converter implementation.
/// </summary>
/// <typeparam name="T">The t type.</typeparam>
/// <typeparam name="TValue">The t value type.</typeparam>
public class UriDictionarySystemTextJsonConverter<T, TValue> :
    JsonConverter<T>
    where T : class, IEnumerable<KeyValuePair<Uri, TValue>>
{
    /// <summary>
    /// Performs the write operation.
    /// </summary>
    /// <param name="writer">The writer value.</param>
    /// <param name="value">The value.</param>
    /// <param name="options">The options value.</param>
    public override void Write(Utf8JsonWriter writer, T value, JsonSerializerOptions options)
    {
        writer.WriteStartObject();

        foreach (KeyValuePair<Uri, TValue> element in value)
        {
            if (element.Key != null)
                writer.WritePropertyName(element.Key.ToString());

            JsonSerializer.Serialize(writer, element.Value, options);
        }

        writer.WriteEndObject();
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
        return ReadInternal(ref reader, typeToConvert, options);
    }

    /// <summary>
    /// Performs the read internal operation.
    /// </summary>
    /// <param name="reader">The reader value.</param>
    /// <param name="typeToConvert">The type to convert value.</param>
    /// <param name="options">The options value.</param>
    /// <returns>The result of the operation.</returns>
    protected T? ReadInternal(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType != JsonTokenType.StartObject)
            throw new JsonException($"Expected StartObject, found: {reader.TokenType}");

        var dictionary = new Dictionary<Uri, TValue>();
        while (reader.Read())
        {
            if (reader.TokenType == JsonTokenType.EndObject)
                return dictionary as T;

            if (reader.TokenType != JsonTokenType.PropertyName)
                throw new JsonException($"Expected PropertyName, found: {reader.TokenType}");

            var propertyName = reader.GetString();

            if (string.IsNullOrWhiteSpace(propertyName))
                throw new JsonException("Expected non-empty PropertyName");

            reader.Read();

            var key = new Uri(propertyName);

            dictionary.Add(key, JsonSerializer.Deserialize<TValue>(ref reader, options)!);
        }

        return dictionary as T;
    }
}
