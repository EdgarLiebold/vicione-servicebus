using System;
using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace ViciOne.ServiceBus.Serialization.JsonConverters;

/// <summary>Converts string decimal json values.</summary>
public class StringDecimalJsonConverter :
    JsonConverter<decimal>
{
    const NumberStyles StringDecimalStyle =
        NumberStyles.AllowLeadingWhite | NumberStyles.AllowTrailingWhite | NumberStyles.AllowLeadingSign |
        NumberStyles.AllowTrailingSign | NumberStyles.AllowDecimalPoint | NumberStyles.AllowThousands |
        NumberStyles.AllowExponent;

    /// <summary>Reads the requested value.</summary>
    /// <param name="reader">The reader updated by the operation.</param>
    /// <param name="typeToConvert">The type to convert.</param>
    /// <param name="options">The options that control the operation.</param>
    /// <returns>The decimal produced by the operation.</returns>
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

    /// <summary>Writes the supplied value.</summary>
    /// <param name="writer">The writer.</param>
    /// <param name="value">The value to process.</param>
    /// <param name="options">The options that control the operation.</param>
    public override void Write(Utf8JsonWriter writer, decimal value, JsonSerializerOptions options)
    {
        var text = Convert.ToString(value, CultureInfo.InvariantCulture);
        if (string.IsNullOrWhiteSpace(text))
            writer.WriteNullValue();
        else
            writer.WriteStringValue(text);
    }
}
