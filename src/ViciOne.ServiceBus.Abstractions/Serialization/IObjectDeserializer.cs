namespace ViciOne.ServiceBus.Advanced.Serialization;

/// <summary>Defines the operations required by object deserializer.</summary>
public interface IObjectDeserializer
{
    /// <summary>Deserializes object.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="value">The value to process.</param>
    /// <param name="defaultValue">The value returned when the requested item is absent.</param>
    /// <returns>The deserialized object.</returns>
    T? DeserializeObject<T>(object? value, T? defaultValue = null)
        where T : class;

    /// <summary>Deserializes object.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="value">The value to process.</param>
    /// <param name="defaultValue">The value returned when the requested item is absent.</param>
    /// <returns>The deserialized object.</returns>
    T? DeserializeObject<T>(object? value, T? defaultValue = null)
        where T : struct;

    /// <summary>Serialize the dictionary to a message body, using the underlying serializer to ensure objects are properly serialized.</summary>
    /// <param name="value">The value to process.</param>
    /// <returns>The serialized object.</returns>
    MessageBody SerializeObject(object? value);
}
