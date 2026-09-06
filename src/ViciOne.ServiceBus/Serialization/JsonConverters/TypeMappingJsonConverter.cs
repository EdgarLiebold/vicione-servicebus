using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace ViciOne.ServiceBus.Serialization.JsonConverters;

/// <summary>Converts type mapping json values.</summary>
/// <typeparam name="TType">The ype type.</typeparam>
/// <typeparam name="TImplementation">The implementation type.</typeparam>
public class TypeMappingJsonConverter<TType, TImplementation> :
    JsonConverter<TType>
    where TImplementation : TType
{
    /// <summary>Reads the requested value.</summary>
    /// <param name="reader">The reader updated by the operation.</param>
    /// <param name="typeToConvert">The type to convert.</param>
    /// <param name="options">The options that control the operation.</param>
    /// <returns>The t type produced by the operation.</returns>
    public override TType? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        return JsonSerializer.Deserialize<TImplementation>(ref reader, options);
    }

    /// <summary>Writes the supplied value.</summary>
    /// <param name="writer">The writer.</param>
    /// <param name="value">The value to process.</param>
    /// <param name="options">The options that control the operation.</param>
    public override void Write(Utf8JsonWriter writer, TType value, JsonSerializerOptions options)
    {
        if (value is TImplementation implementation)
            JsonSerializer.Serialize(writer, implementation, options);
        else if (value is object obj)
            JsonSerializer.Serialize(writer, obj, obj.GetType(), options);
    }
}
