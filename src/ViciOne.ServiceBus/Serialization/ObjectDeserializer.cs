#nullable enable
namespace ViciOne.ServiceBus.Serialization;

/// <summary>
/// Converts ServiceBus-owned infrastructure values using the stable metadata codec. This helper intentionally
/// has no mutable or ambient serializer state; message payload conversion belongs to the concrete SerializerContext.
/// </summary>
public static class ObjectDeserializer
{
    public static string? Serialize(object? value)
    {
        return value == null
            ? null
            : ServiceBusMetadataJson.ObjectDeserializer.SerializeObject(value).GetString();
    }

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
