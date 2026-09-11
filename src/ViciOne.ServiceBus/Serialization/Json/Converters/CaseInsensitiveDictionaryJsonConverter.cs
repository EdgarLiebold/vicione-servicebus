using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace ViciOne.ServiceBus.Serialization.Json.Converters;

/// <summary>Reads string-keyed JSON objects into dictionaries with ordinal case-insensitive keys.</summary>
/// <typeparam name="T">A dictionary-compatible declared type.</typeparam>
/// <typeparam name="TValue">The dictionary value type.</typeparam>
internal sealed class CaseInsensitiveDictionaryJsonConverter<T, TValue> :
    JsonConverter<T>
    where T : class, IEnumerable<KeyValuePair<string, TValue>>
{
    /// <summary>Writes the dictionary as a JSON object.</summary>
    /// <param name="writer">The destination writer.</param>
    /// <param name="value">The dictionary entries.</param>
    /// <param name="options">The serializer options used for each value.</param>
    public override void Write(Utf8JsonWriter writer, T value, JsonSerializerOptions options)
    {
        writer.WriteStartObject();

        foreach (KeyValuePair<string, TValue> element in value)
        {
            if (string.IsNullOrWhiteSpace(element.Key))
                throw new JsonException("Dictionary keys must be non-empty JSON property names.");

            writer.WritePropertyName(element.Key);
            JsonSerializer.Serialize(writer, element.Value, options);
        }

        writer.WriteEndObject();
    }

    /// <summary>Reads a JSON object into a case-insensitive dictionary.</summary>
    /// <param name="reader">The reader positioned at the object.</param>
    /// <param name="typeToConvert">The declared dictionary-compatible type.</param>
    /// <param name="options">The serializer options used for each value.</param>
    /// <returns>The deserialized dictionary.</returns>
    public override T? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType != JsonTokenType.StartObject)
            throw new JsonException($"Expected StartObject, found: {reader.TokenType}");

        var dictionary = new Dictionary<string, TValue>(StringComparer.OrdinalIgnoreCase);
        while (reader.Read())
        {
            if (reader.TokenType == JsonTokenType.EndObject)
                return dictionary as T
                    ?? throw new JsonException($"Dictionary type '{typeToConvert}' cannot be materialized by this converter.");

            if (reader.TokenType != JsonTokenType.PropertyName)
                throw new JsonException($"Expected PropertyName, found: {reader.TokenType}");

            var propertyName = reader.GetString();

            if (string.IsNullOrWhiteSpace(propertyName))
                throw new JsonException("Expected non-empty PropertyName");

            if (!reader.Read())
                throw new JsonException("The JSON object ended before the dictionary value was read.");

            dictionary[propertyName] = JsonSerializer.Deserialize<TValue>(ref reader, options)!;
        }

        throw new JsonException("The JSON object ended before its closing token was read.");
    }
}
