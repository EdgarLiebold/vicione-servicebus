namespace ViciOne.ServiceBus.Advanced.Serialization;

/// <summary>
/// Defines the contract for object deserializer.
/// </summary>
public interface IObjectDeserializer
{
    /// <summary>
    /// Performs the deserialize object operation.
    /// </summary>
    /// <typeparam name="T">The t type.</typeparam>
    /// <param name="value">The value.</param>
    /// <param name="defaultValue">The default value value.</param>
    /// <returns>The result of the operation.</returns>
    T? DeserializeObject<T>(object? value, T? defaultValue = null)
        where T : class;

    /// <summary>
    /// Performs the deserialize object operation.
    /// </summary>
    /// <typeparam name="T">The t type.</typeparam>
    /// <param name="value">The value.</param>
    /// <param name="defaultValue">The default value value.</param>
    /// <returns>The result of the operation.</returns>
    T? DeserializeObject<T>(object? value, T? defaultValue = null)
        where T : struct;

    /// <summary>
    /// Serialize the dictionary to a message body, using the underlying serializer to ensure objects are properly serialized.
    /// </summary>
    /// <param name="value"></param>
    /// <returns></returns>
    MessageBody SerializeObject(object? value);
}
