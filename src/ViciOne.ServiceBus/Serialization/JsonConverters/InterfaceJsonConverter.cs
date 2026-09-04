using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace ViciOne.ServiceBus.Serialization.JsonConverters;

/// <summary>
/// Provides an interface json converter implementation.
/// </summary>
/// <typeparam name="TType">The t type type.</typeparam>
/// <typeparam name="TImplementation">The t implementation type.</typeparam>
public class InterfaceJsonConverter<TType, TImplementation> :
    JsonConverter<TType>
    where TImplementation : TType
{
    /// <summary>
    /// Performs the read operation.
    /// </summary>
    /// <param name="reader">The reader value.</param>
    /// <param name="typeToConvert">The type to convert value.</param>
    /// <param name="options">The options value.</param>
    /// <returns>The result of the operation.</returns>
    public override TType? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        return JsonSerializer.Deserialize<TImplementation>(ref reader, options);
    }

    /// <summary>
    /// Performs the write operation.
    /// </summary>
    /// <param name="writer">The writer value.</param>
    /// <param name="value">The value.</param>
    /// <param name="options">The options value.</param>
    public override void Write(Utf8JsonWriter writer, TType value, JsonSerializerOptions options)
    {
        if (value == null)
        {
            writer.WriteNullValue();
            return;
        }

        JsonSerializer.Serialize(writer, value, value.GetType(), options);
    }
}
