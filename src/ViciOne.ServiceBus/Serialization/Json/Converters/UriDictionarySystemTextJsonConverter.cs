using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace ViciOne.ServiceBus.Serialization.Json.Converters;

/// <summary>Maps URI-keyed dictionaries to JSON objects whose property names contain the URI text.</summary>
/// <typeparam name="T">A dictionary-compatible declared type.</typeparam>
/// <typeparam name="TValue">The dictionary value type.</typeparam>
internal sealed class UriDictionarySystemTextJsonConverter<T, TValue> :
    JsonConverter<T>
    where T : class, IEnumerable<KeyValuePair<Uri, TValue>>
{
    /// <summary>Writes URI keys as JSON property names.</summary>
    /// <param name="writer">The destination writer.</param>
    /// <param name="value">The dictionary entries.</param>
    /// <param name="options">The serializer options used for each value.</param>
    public override void Write(Utf8JsonWriter writer, T value, JsonSerializerOptions options)
    {
        writer.WriteStartObject();

        foreach (KeyValuePair<Uri, TValue> element in value)
        {
            if (element.Key is null)
                throw new JsonException("URI dictionary keys cannot be null.");

            writer.WritePropertyName(element.Key.ToString());
            JsonSerializer.Serialize(writer, element.Value, options);
        }

        writer.WriteEndObject();
    }

    /// <summary>Reads JSON property names as URI keys.</summary>
    /// <param name="reader">The reader positioned at the object.</param>
    /// <param name="typeToConvert">The declared dictionary-compatible type.</param>
    /// <param name="options">The serializer options used for each value.</param>
    /// <returns>The deserialized dictionary.</returns>
    public override T? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType != JsonTokenType.StartObject)
            throw new JsonException($"Expected StartObject, found: {reader.TokenType}");

        var dictionary = new Dictionary<Uri, TValue>();
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

            if (!Uri.TryCreate(propertyName, UriKind.RelativeOrAbsolute, out Uri? key))
                throw new JsonException($"Property name '{propertyName}' is not a valid URI.");

            dictionary[key] = JsonSerializer.Deserialize<TValue>(ref reader, options)!;
        }

        throw new JsonException("The JSON object ended before its closing token was read.");
    }
}
