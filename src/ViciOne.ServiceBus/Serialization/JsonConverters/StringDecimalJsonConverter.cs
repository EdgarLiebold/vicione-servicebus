using System;
using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace ViciOne.ServiceBus.Serialization.JsonConverters;

/// <summary>
/// Provides a string decimal json converter implementation.
/// </summary>
public class StringDecimalJsonConverter :
    JsonConverter<decimal>
{
    const NumberStyles StringDecimalStyle =
        NumberStyles.AllowLeadingWhite | NumberStyles.AllowTrailingWhite | NumberStyles.AllowLeadingSign |
        NumberStyles.AllowTrailingSign | NumberStyles.AllowDecimalPoint | NumberStyles.AllowThousands |
        NumberStyles.AllowExponent;

    /// <summary>
    /// Performs the read operation.
    /// </summary>
    /// <param name="reader">The reader value.</param>
    /// <param name="typeToConvert">The type to convert value.</param>
    /// <param name="options">The options value.</param>
    /// <returns>The result of the operation.</returns>
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

    /// <summary>
    /// Performs the write operation.
    /// </summary>
    /// <param name="writer">The writer value.</param>
    /// <param name="value">The value.</param>
    /// <param name="options">The options value.</param>
    public override void Write(Utf8JsonWriter writer, decimal value, JsonSerializerOptions options)
    {
        var text = Convert.ToString(value, CultureInfo.InvariantCulture);
        if (string.IsNullOrWhiteSpace(text))
            writer.WriteNullValue();
        else
            writer.WriteStringValue(text);
    }
}
