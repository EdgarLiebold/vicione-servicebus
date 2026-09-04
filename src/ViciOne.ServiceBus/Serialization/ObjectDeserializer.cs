#nullable enable
namespace ViciOne.ServiceBus.Serialization;

/// <summary>
/// Converts ServiceBus-owned infrastructure values using the stable metadata codec. This helper intentionally
/// has no mutable or ambient serializer state; message payload conversion belongs to the concrete SerializerContext.
/// </summary>
public static class ObjectDeserializer
{
    /// <summary>
    /// Performs the serialize operation.
    /// </summary>
    /// <param name="value">The value.</param>
    /// <returns>The result of the operation.</returns>
    public static string? Serialize(object? value)
    {
        return value == null
            ? null
            : ServiceBusMetadataJson.ObjectDeserializer.SerializeObject(value).GetString();
    }

    /// <summary>
    /// Performs the deserialize operation.
    /// </summary>
    /// <typeparam name="T">The t type.</typeparam>
    /// <param name="value">The value.</param>
    /// <param name="defaultValue">The default value value.</param>
    /// <returns>The result of the operation.</returns>
    public static T? Deserialize<T>(object? value, T? defaultValue = null)
        where T : class
    {
        return value switch
        {
            null => defaultValue,
            string text when string.IsNullOrWhiteSpace(text) => defaultValue,
            _ => ServiceBusMetadataJson.ObjectDeserializer.DeserializeObject<T>(value, defaultValue)
        };
    }

    /// <summary>
    /// Performs the deserialize operation.
    /// </summary>
    /// <typeparam name="T">The t type.</typeparam>
    /// <param name="value">The value.</param>
    /// <param name="defaultValue">The default value value.</param>
    /// <returns>The result of the operation.</returns>
    public static T? Deserialize<T>(object? value, T? defaultValue = default)
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
