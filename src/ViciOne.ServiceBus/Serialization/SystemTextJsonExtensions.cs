using System;
using System.Text.Json;
using ViciOne.ServiceBus.Metadata;

namespace ViciOne.ServiceBus.Serialization;

/// <summary>
/// Provides extension methods for system text json.
/// </summary>
public static class SystemTextJsonExtensions
{
    /// <summary>
    /// Gets object.
    /// </summary>
    /// <typeparam name="T">The t type.</typeparam>
    /// <param name="jsonElement">The json element value.</param>
    /// <param name="options">The options value.</param>
    /// <returns>The result of the operation.</returns>
    public static T? GetObject<T>(this JsonElement jsonElement, JsonSerializerOptions options)
        where T : class
    {
        if (typeof(T).IsInterface && MessageTypeCache<T>.IsValidMessageType)
        {
            var messageType = TypeMetadataCache<T>.ImplementationType;

            if (jsonElement.Deserialize(messageType, options) is T obj)
                return obj;
        }

        return jsonElement.Deserialize<T>(options);
    }

    /// <summary>
    /// Performs the transform operation.
    /// </summary>
    /// <typeparam name="T">The t type.</typeparam>
    /// <param name="objectToTransform">The object to transform value.</param>
    /// <param name="options">The options value.</param>
    /// <returns>The result of the operation.</returns>
    public static T? Transform<T>(this object objectToTransform, JsonSerializerOptions options)
        where T : class
    {
        var jsonElement = JsonSerializer.SerializeToElement(objectToTransform, options);

        return jsonElement.GetObject<T>(options);
    }

    /// <summary>
    /// Performs the transform operation.
    /// </summary>
    /// <param name="objectToTransform">The object to transform value.</param>
    /// <param name="targetType">The target type value.</param>
    /// <param name="options">The options value.</param>
    /// <returns>The result of the operation.</returns>
    public static object? Transform(this object objectToTransform, Type targetType, JsonSerializerOptions options)
    {
        var jsonElement = JsonSerializer.SerializeToElement(objectToTransform, options);

        if (targetType.IsInterface && MessageTypeCache.IsValidMessageType(targetType))
            targetType = TypeMetadataCache.GetImplementationType(targetType);

        return jsonElement.Deserialize(targetType, options);
    }
}
