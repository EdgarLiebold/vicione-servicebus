using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace ViciOne.ServiceBus.Serialization.Json.Converters;

/// <summary>Preserves transport-safe scalar values when reading and writing string/object dictionaries.</summary>
/// <typeparam name="T">A dictionary-compatible declared type.</typeparam>
internal sealed class CaseInsensitiveDictionaryStringObjectJsonConverter<T> :
    JsonConverter<T>
    where T : class, IEnumerable<KeyValuePair<string, object>>
{
    /// <summary>Reads an object or key/value array into a case-insensitive dictionary.</summary>
    /// <param name="reader">The reader positioned at the dictionary value.</param>
    /// <param name="typeToConvert">The declared dictionary-compatible type.</param>
    /// <param name="options">The serializer options used for nested values.</param>
    /// <returns>The deserialized dictionary.</returns>
    public override T? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        switch (reader.TokenType)
        {
            case JsonTokenType.Null:
                return new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase) as T;

            case JsonTokenType.StartObject:
                return ReadObject(ref reader, options) as T;

            case JsonTokenType.StartArray:
                return ReadArray(ref reader, options) as T;

            default:
                throw new JsonException($"Expected StartObject or StartArray, found: {reader.TokenType}");
        }
    }

    /// <summary>Writes dictionary entries while preserving supported scalar types.</summary>
    /// <param name="writer">The destination writer.</param>
    /// <param name="value">The dictionary entries.</param>
    /// <param name="options">The serializer options used for nested values.</param>
    public override void Write(Utf8JsonWriter writer, T value, JsonSerializerOptions options)
    {
        WriteEntries(writer, value, options);
    }

    static void WriteEntries(Utf8JsonWriter writer, IEnumerable<KeyValuePair<string, object>> values, JsonSerializerOptions options)
    {
        writer.WriteStartObject();

        foreach (KeyValuePair<string, object> element in values)
            WriteValue(writer, element.Key, element.Value, options);

        writer.WriteEndObject();
    }

    static void WriteValue(Utf8JsonWriter writer, string key, object objectValue, JsonSerializerOptions options)
    {
        if (string.IsNullOrWhiteSpace(key))
            throw new JsonException("Dictionary keys must be non-empty JSON property names.");

        if (ShouldIgnore(objectValue, options.DefaultIgnoreCondition))
            return;

        writer.WritePropertyName(key);
        WritePropertyValue(writer, objectValue, options);
    }

    static bool ShouldIgnore(object? value, JsonIgnoreCondition ignoreCondition)
    {
        if (value == null)
            return ignoreCondition is JsonIgnoreCondition.WhenWritingNull or JsonIgnoreCondition.WhenWritingDefault;

        return ignoreCondition == JsonIgnoreCondition.WhenWritingDefault
            && value.GetType() is { IsValueType: true } valueType
            && value.Equals(Activator.CreateInstance(valueType));
    }

    static void WritePropertyValue(Utf8JsonWriter writer, object? value, JsonSerializerOptions options)
    {
        switch (value)
        {
            case null:
                writer.WriteNullValue();
                break;
            case string text:
                writer.WriteStringValue(text);
                break;
            case DateTime dateTime:
                writer.WriteStringValue(dateTime);
                break;
            case DateTimeOffset dateTimeOffset:
                writer.WriteStringValue(dateTimeOffset);
                break;
            case Guid guid:
                writer.WriteStringValue(guid.ToString("D"));
                break;
            case long number:
                writer.WriteNumberValue(number);
                break;
            case int number:
                writer.WriteNumberValue(number);
                break;
            case short number:
                writer.WriteNumberValue(number);
                break;
            case byte number:
                writer.WriteNumberValue(number);
                break;
            case float number:
                writer.WriteNumberValue(number);
                break;
            case double number:
                writer.WriteNumberValue(number);
                break;
            case decimal number:
                writer.WriteStringValue(Convert.ToString(number, CultureInfo.InvariantCulture));
                break;
            case bool flag:
                writer.WriteBooleanValue(flag);
                break;

            default:
                JsonSerializer.Serialize(writer, value, options);
                break;
        }
    }

    static Dictionary<string, object> ReadObject(ref Utf8JsonReader reader, JsonSerializerOptions options)
    {
        var ignoreDefault = options.DefaultIgnoreCondition == JsonIgnoreCondition.WhenWritingDefault;
        var ignoreNull = ignoreDefault || options.DefaultIgnoreCondition == JsonIgnoreCondition.WhenWritingNull;

        var dictionary = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
        while (reader.Read())
        {
            if (reader.TokenType == JsonTokenType.EndObject)
                return dictionary;

            if (reader.TokenType != JsonTokenType.PropertyName)
                throw new JsonException($"Expected PropertyName, found: {reader.TokenType}");

            var propertyName = reader.GetString();

            if (string.IsNullOrWhiteSpace(propertyName))
                throw new JsonException("Expected non-empty PropertyName");

            if (!reader.Read())
                throw new JsonException("The JSON object ended before the dictionary value was read.");

            var value = ReadPropertyValue(ref reader, options);
            if (value != null || !ignoreNull)
                dictionary[propertyName] = value!;
        }

        throw new JsonException("The JSON object ended before its closing token was read.");
    }

    static Dictionary<string, object> ReadArray(ref Utf8JsonReader reader, JsonSerializerOptions options)
    {
        var ignoreDefault = options.DefaultIgnoreCondition == JsonIgnoreCondition.WhenWritingDefault;
        var ignoreNull = ignoreDefault || options.DefaultIgnoreCondition == JsonIgnoreCondition.WhenWritingNull;

        var dictionary = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
        while (reader.Read())
        {
            if (reader.TokenType == JsonTokenType.EndArray)
                return dictionary;

            if (reader.TokenType == JsonTokenType.StartObject)
            {
                Dictionary<string, object> elementDictionary = ReadObject(ref reader, options);
                if (elementDictionary.TryGetValue("Key", out var keyValue) && keyValue is string key && !string.IsNullOrWhiteSpace(key)
                    && elementDictionary.TryGetValue("Value", out var value))
                {
                    if (value != null || !ignoreNull)
                        dictionary[key] = value!;
                }
            }
            else
                throw new JsonException($"Expected object (key/value), found: {reader.TokenType}");
        }

        throw new JsonException("The JSON array ended before its closing token was read.");
    }

    static object? ReadPropertyValue(ref Utf8JsonReader reader, JsonSerializerOptions options)
    {
        switch (reader.TokenType)
        {
            case JsonTokenType.String:
                return reader.GetString();

            case JsonTokenType.False:
                return false;

            case JsonTokenType.True:
                return true;

            case JsonTokenType.Null:
                return null;

            case JsonTokenType.Number:
                if (reader.TryGetInt64(out var result))
                    return result;

                return reader.GetDouble();

            case JsonTokenType.StartObject:
                return ReadObject(ref reader, options);

            case JsonTokenType.StartArray:
                return ReadList(ref reader, options);

            default:
                throw new JsonException($"Unsupported JsonTokenType found: {reader.TokenType}");
        }
    }

    static List<object> ReadList(ref Utf8JsonReader reader, JsonSerializerOptions options)
    {
        var values = new List<object>();
        while (reader.Read())
        {
            if (reader.TokenType == JsonTokenType.EndArray)
                return values;

            values.Add(ReadPropertyValue(ref reader, options)!);
        }

        throw new JsonException("The JSON array ended before its closing token was read.");
    }
}
