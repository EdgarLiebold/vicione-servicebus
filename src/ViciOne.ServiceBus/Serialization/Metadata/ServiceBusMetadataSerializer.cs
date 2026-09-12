namespace ViciOne.ServiceBus.Serialization;

/// <summary>
/// Converts ServiceBus-owned infrastructure values using the stable metadata codec without mutable or ambient
/// serializer state. Message payload conversion belongs to the concrete <see cref="SerializerContext" />.
/// </summary>
public static class ServiceBusMetadataSerializer
{
    /// <summary>Serializes a ServiceBus-owned infrastructure value with the stable metadata codec.</summary>
    /// <param name="value">The value to serialize.</param>
    /// <returns>The JSON text, or <see langword="null" /> when <paramref name="value" /> is null.</returns>
    public static string? Serialize(object? value)
    {
        return value == null
            ? null
            : ServiceBusMetadataJson.ObjectDeserializer.SerializeObject(value).GetRequiredTransportText();
    }

    /// <summary>Converts infrastructure metadata to a reference type.</summary>
    /// <typeparam name="T">The requested reference type.</typeparam>
    /// <param name="value">The serialized or native metadata value.</param>
    /// <param name="defaultValue">The value returned when the requested item is absent.</param>
    /// <returns>The converted value or <paramref name="defaultValue" />.</returns>
    public static T? DeserializeReference<T>(object? value, T? defaultValue = null)
        where T : class
    {
        return value switch
        {
            null => defaultValue,
            string text when string.IsNullOrWhiteSpace(text) => defaultValue,
            _ => ServiceBusMetadataJson.ObjectDeserializer.DeserializeObject<T>(value, defaultValue)
        };
    }

    /// <summary>Converts infrastructure metadata to an optional value type.</summary>
    /// <typeparam name="T">The requested value type.</typeparam>
    /// <param name="value">The serialized or native metadata value.</param>
    /// <returns>The converted value, or <see langword="null" /> when the requested item is absent.</returns>
    public static T? DeserializeValue<T>(object? value)
        where T : struct
    {
        return DeserializeOptionalValue<T>(value, null);
    }

    /// <summary>Converts infrastructure metadata to a value type with an explicit absence fallback.</summary>
    /// <typeparam name="T">The requested value type.</typeparam>
    /// <param name="value">The serialized or native metadata value.</param>
    /// <param name="defaultValue">The value returned when the requested item is absent.</param>
    /// <returns>The converted value or <paramref name="defaultValue" />.</returns>
    public static T DeserializeValue<T>(object? value, T defaultValue)
        where T : struct
    {
        return DeserializeOptionalValue<T>(value, defaultValue) ?? defaultValue;
    }

    static T? DeserializeOptionalValue<T>(object? value, T? defaultValue)
        where T : struct
    {
        return value switch
        {
            null => defaultValue,
            string text when string.IsNullOrWhiteSpace(text) => defaultValue,
            _ => ServiceBusMetadataJson.ObjectDeserializer.DeserializeObject<T>(value, defaultValue)
        };
    }
}
