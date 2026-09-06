using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace ViciOne.ServiceBus.Serialization.JsonConverters;

/// <summary>Serializes a contract through its registered concrete implementation.</summary>
/// <typeparam name="TType">The message contract type.</typeparam>
/// <typeparam name="TImplementation">The concrete serialized representation.</typeparam>
public sealed class TypeMappingJsonConverter<TType, TImplementation> :
    JsonConverter<TType>
    where TImplementation : TType
{
    /// <summary>Deserializes the concrete representation as the contract type.</summary>
    /// <param name="reader">The JSON reader positioned at the value.</param>
    /// <param name="typeToConvert">The requested contract type.</param>
    /// <param name="options">The serializer options.</param>
    /// <returns>The deserialized contract value.</returns>
    public override TType? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        return JsonSerializer.Deserialize<TImplementation>(ref reader, options);
    }

    /// <summary>Serializes the concrete representation or the supplied runtime implementation.</summary>
    /// <param name="writer">The destination JSON writer.</param>
    /// <param name="value">The contract value to serialize.</param>
    /// <param name="options">The serializer options.</param>
    public override void Write(Utf8JsonWriter writer, TType value, JsonSerializerOptions options)
    {
        if (value is TImplementation implementation)
            JsonSerializer.Serialize(writer, implementation, options);
        else if (value is object obj)
            JsonSerializer.Serialize(writer, obj, obj.GetType(), options);
    }
}
