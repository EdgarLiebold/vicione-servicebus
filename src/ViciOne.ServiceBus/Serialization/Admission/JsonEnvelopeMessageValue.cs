using System;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;
using ViciOne.ServiceBus.Serialization;

namespace ViciOne.ServiceBus.Advanced.Serialization;

/// <summary>Locates the exact application value in a bounded JSON envelope.</summary>
internal static class JsonEnvelopeMessageValue
{
    internal static ReadOnlyMemory<byte> Extract(ReadOnlyMemory<byte> envelope, JsonSerializerOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        string propertyName = GetPropertyName(options);
        var reader = new Utf8JsonReader(envelope.Span, new JsonReaderOptions
        {
            AllowTrailingCommas = options.AllowTrailingCommas,
            CommentHandling = options.ReadCommentHandling,
            MaxDepth = options.MaxDepth,
        });

        if (!reader.Read() || reader.TokenType != JsonTokenType.StartObject)
            throw new InvalidOperationException("The copied JSON envelope is not an object.");

        long start = -1;
        long length = -1;
        while (reader.Read())
        {
            if (reader.TokenType != JsonTokenType.PropertyName || reader.CurrentDepth != 1)
                continue;

            string? name = reader.GetString();
            if (!string.Equals(name, propertyName,
                    options.PropertyNameCaseInsensitive ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal))
                continue;

            if (start >= 0 || !ReadNextValue(ref reader))
                throw new InvalidOperationException("The copied JSON envelope has no unique message value.");

            start = reader.TokenStartIndex;
            reader.Skip();
            length = reader.BytesConsumed - start;
        }

        if (start < 0 || length < 0)
            throw new InvalidOperationException("The copied JSON envelope has no message value.");

        return envelope.Slice((int)start, (int)length);
    }

    private static bool ReadNextValue(ref Utf8JsonReader reader)
    {
        while (reader.Read())
        {
            if (reader.TokenType != JsonTokenType.Comment)
                return true;
        }

        return false;
    }

    private static string GetPropertyName(JsonSerializerOptions options)
    {
        PropertyInfo property = typeof(JsonMessageEnvelope).GetProperty(nameof(JsonMessageEnvelope.Message))!;
        string expectedName = property.GetCustomAttribute<JsonPropertyNameAttribute>()?.Name
            ?? options.PropertyNamingPolicy?.ConvertName(property.Name)
            ?? property.Name;

        JsonTypeInfo typeInfo = options.GetTypeInfo(typeof(JsonMessageEnvelope));
        foreach (JsonPropertyInfo jsonProperty in typeInfo.Properties)
        {
            if (jsonProperty.AttributeProvider is MemberInfo member && member.Name == property.Name)
                return jsonProperty.Name;
        }

        foreach (JsonPropertyInfo jsonProperty in typeInfo.Properties)
        {
            if (string.Equals(jsonProperty.Name, expectedName, StringComparison.Ordinal))
                return jsonProperty.Name;
        }

        throw new InvalidOperationException("The configured JSON envelope has no identifiable message property.");
    }
}
