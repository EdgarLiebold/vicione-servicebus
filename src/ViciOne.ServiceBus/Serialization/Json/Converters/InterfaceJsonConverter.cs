using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace ViciOne.ServiceBus.Serialization.Json.Converters;

/// <summary>Deserializes an interface contract through its generated implementation type.</summary>
/// <typeparam name="TType">The interface contract.</typeparam>
/// <typeparam name="TImplementation">The generated concrete representation.</typeparam>
internal sealed class InterfaceJsonConverter<TType, TImplementation> :
    JsonConverter<TType>
    where TImplementation : TType
{
    /// <summary>Reads the generated implementation and returns it through the interface contract.</summary>
    /// <param name="reader">The reader positioned at the contract value.</param>
    /// <param name="typeToConvert">The requested interface contract.</param>
    /// <param name="options">The active serializer options.</param>
    /// <returns>The deserialized contract value.</returns>
    public override TType? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        return JsonSerializer.Deserialize<TImplementation>(ref reader, options);
    }

    /// <summary>Writes the runtime implementation of an interface message.</summary>
    /// <param name="writer">The destination writer.</param>
    /// <param name="value">The interface message.</param>
    /// <param name="options">The active serializer options.</param>
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
