using System;
using System.Text.Json;
using ViciOne.ServiceBus.Internals.Reflection;

namespace ViciOne.ServiceBus.Serialization;

/// <summary>Converts message values through the configured System.Text.Json contract model.</summary>
public static class SystemTextJsonExtensions
{
    /// <summary>Deserializes a JSON value, using the generated implementation for interface message contracts.</summary>
    /// <typeparam name="T">The requested message contract.</typeparam>
    /// <param name="jsonElement">The JSON value to deserialize.</param>
    /// <param name="options">The serializer options.</param>
    /// <returns>The deserialized message, or <see langword="null" /> for a JSON null value.</returns>
    public static T? GetObject<T>(this JsonElement jsonElement, JsonSerializerOptions options)
        where T : class
    {
        ArgumentNullException.ThrowIfNull(options);
        if (typeof(T).IsInterface && MessageTypeCache<T>.IsValidMessageType)
        {
            var messageType = MessageImplementationCache<T>.ImplementationType;

            if (jsonElement.Deserialize(messageType, options) is T obj)
                return obj;
        }

        return jsonElement.Deserialize<T>(options);
    }

    /// <summary>Projects an object into another message contract through JSON.</summary>
    /// <typeparam name="T">The target message contract.</typeparam>
    /// <param name="objectToTransform">The source object.</param>
    /// <param name="options">The serializer options.</param>
    /// <returns>The projected message, or <see langword="null" /> for a JSON null value.</returns>
    public static T? Transform<T>(this object objectToTransform, JsonSerializerOptions options)
        where T : class
    {
        ArgumentNullException.ThrowIfNull(objectToTransform);
        ArgumentNullException.ThrowIfNull(options);
        var jsonElement = JsonSerializer.SerializeToElement(objectToTransform, options);

        return jsonElement.GetObject<T>(options);
    }

    /// <summary>Projects an object into a runtime message contract through JSON.</summary>
    /// <param name="objectToTransform">The source object.</param>
    /// <param name="targetType">The target message contract.</param>
    /// <param name="options">The serializer options.</param>
    /// <returns>The projected message, or <see langword="null" /> for a JSON null value.</returns>
    public static object? Transform(this object objectToTransform, Type targetType, JsonSerializerOptions options)
    {
        ArgumentNullException.ThrowIfNull(objectToTransform);
        ArgumentNullException.ThrowIfNull(targetType);
        ArgumentNullException.ThrowIfNull(options);
        var jsonElement = JsonSerializer.SerializeToElement(objectToTransform, options);

        if (targetType.IsInterface && MessageTypeCache.IsValidMessageType(targetType))
            targetType = MessageImplementationCache.GetImplementationType(targetType);

        return jsonElement.Deserialize(targetType, options);
    }
}
