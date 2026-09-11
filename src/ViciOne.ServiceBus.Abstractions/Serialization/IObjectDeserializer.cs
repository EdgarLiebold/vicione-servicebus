namespace ViciOne.ServiceBus.Advanced.Serialization;

/// <summary>Converts serializer-specific values to message contract values and bodies.</summary>
public interface IObjectDeserializer
{
    /// <summary>Deserializes a value as a reference type.</summary>
    /// <typeparam name="TValue">The requested result type.</typeparam>
    /// <param name="value">The serializer-specific source value.</param>
    /// <param name="defaultValue">The fallback returned when the source cannot produce the requested value.</param>
    /// <returns>The deserialized value, or <paramref name="defaultValue" />.</returns>
    TValue? DeserializeObject<TValue>(object? value, TValue? defaultValue = null)
        where TValue : class;

    /// <summary>Deserializes a value as a nullable value type.</summary>
    /// <typeparam name="TValue">The requested result type.</typeparam>
    /// <param name="value">The serializer-specific source value.</param>
    /// <param name="defaultValue">The fallback returned when the source cannot produce the requested value.</param>
    /// <returns>The deserialized value, or <paramref name="defaultValue" />.</returns>
    TValue? DeserializeObject<TValue>(object? value, TValue? defaultValue = null)
        where TValue : struct;

    /// <summary>Serializes a value into the body representation owned by this serializer.</summary>
    /// <param name="value">The value to serialize.</param>
    /// <returns>The serialized message body.</returns>
    MessageBody SerializeObject(object? value);
}
