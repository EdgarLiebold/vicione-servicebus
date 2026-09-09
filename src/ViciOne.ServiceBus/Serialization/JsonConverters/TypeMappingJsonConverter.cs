using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace ViciOne.ServiceBus.Serialization.JsonConverters;

/// <summary>Deserializes a contract through its registered concrete representation and preserves runtime representations when writing.</summary>
/// <typeparam name="TContract">The message contract type.</typeparam>
/// <typeparam name="TImplementation">The concrete serialized representation.</typeparam>
public sealed class TypeMappingJsonConverter<TContract, TImplementation> :
    JsonConverter<TContract>
    where TImplementation : TContract
{
    /// <summary>Deserializes the concrete representation as the contract type.</summary>
    /// <param name="reader">The JSON reader positioned at the value.</param>
    /// <param name="typeToConvert">The requested contract type.</param>
    /// <param name="options">The serializer options.</param>
    /// <returns>The deserialized contract value.</returns>
    public override TContract? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        return JsonSerializer.Deserialize<TImplementation>(ref reader, options);
    }

    /// <summary>Serializes the concrete representation or the supplied runtime implementation.</summary>
    /// <param name="writer">The destination JSON writer.</param>
    /// <param name="value">The contract value to serialize.</param>
    /// <param name="options">The serializer options.</param>
    public override void Write(Utf8JsonWriter writer, TContract value, JsonSerializerOptions options)
    {
        if (value is null)
            writer.WriteNullValue();
        else if (value is TImplementation implementation)
            JsonSerializer.Serialize(writer, implementation, options);
        else
            JsonSerializer.Serialize(writer, value, value.GetType(), options);
    }
}
