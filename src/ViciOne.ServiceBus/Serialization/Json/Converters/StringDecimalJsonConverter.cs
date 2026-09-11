using System;
using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace ViciOne.ServiceBus.Serialization.Json.Converters;

/// <summary>Writes decimals as invariant strings and accepts invariant string or numeric input.</summary>
internal sealed class StringDecimalJsonConverter :
    JsonConverter<decimal>
{
    const NumberStyles StringDecimalStyle =
        NumberStyles.AllowLeadingWhite | NumberStyles.AllowTrailingWhite | NumberStyles.AllowLeadingSign |
        NumberStyles.AllowTrailingSign | NumberStyles.AllowDecimalPoint | NumberStyles.AllowThousands |
        NumberStyles.AllowExponent;

    /// <summary>Reads an invariant decimal from a JSON string, number, or null token.</summary>
    /// <param name="reader">The reader positioned at the decimal value.</param>
    /// <param name="typeToConvert">The decimal target type.</param>
    /// <param name="options">The active serializer options.</param>
    /// <returns>The parsed decimal, or zero for null and blank string values.</returns>
    public override decimal Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.Null)
            return default;

        if (reader.TokenType == JsonTokenType.Number && reader.TryGetDecimal(out var value))
            return value;

        if (reader.TokenType == JsonTokenType.String)
        {
            var text = reader.GetString();

            if (string.IsNullOrWhiteSpace(text))
                return default;

            if (decimal.TryParse(text, StringDecimalStyle, CultureInfo.InvariantCulture, out var result))
                return result;
        }

        throw new JsonException($"Expected String, Number or Null, found: {reader.TokenType}");
    }

    /// <summary>Writes a decimal as invariant JSON string text.</summary>
    /// <param name="writer">The destination writer.</param>
    /// <param name="value">The decimal value.</param>
    /// <param name="options">The active serializer options.</param>
    public override void Write(Utf8JsonWriter writer, decimal value, JsonSerializerOptions options)
    {
        var text = Convert.ToString(value, CultureInfo.InvariantCulture);
        if (string.IsNullOrWhiteSpace(text))
            writer.WriteNullValue();
        else
            writer.WriteStringValue(text);
    }
}
