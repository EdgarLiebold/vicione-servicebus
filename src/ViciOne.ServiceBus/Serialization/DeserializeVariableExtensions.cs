using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace ViciOne.ServiceBus.Serialization;

/// <summary>Provides extension methods for deserialize variable.</summary>
public static class DeserializeVariableExtensions
{
    /// <summary>Return an object from the dictionary converted to T using the message deserializer.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="dictionary">The dictionary.</param>
    /// <param name="key">The key used to identify the requested entry.</param>
    /// <param name="value">Receives the value produced by the operation.</param>
    /// <returns><see langword="true" /> when the condition is satisfied; otherwise, <see langword="false" />.</returns>
    public static bool TryGetValue<T>(this IDictionary<string, object?>? dictionary, string key, [NotNullWhen(true)] out T? value)
        where T : class
    {
        if (dictionary is null)
        {
            value = null;
            return false;
        }

        return ServiceBusMetadataJson.ObjectDeserializer.TryGetValue(AsNonNullableDictionary(dictionary), key, out value);
    }

    /// <summary>Return an object from the dictionary converted to T using the message deserializer.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="dictionary">The dictionary.</param>
    /// <param name="key">The key used to identify the requested entry.</param>
    /// <param name="value">Receives the value produced by the operation.</param>
    /// <returns><see langword="true" /> when the condition is satisfied; otherwise, <see langword="false" />.</returns>
    public static bool TryGetValue<T>(this IDictionary<string, object?>? dictionary, string key, [NotNullWhen(true)] out T? value)
        where T : struct
    {
        if (dictionary is null)
        {
            value = null;
            return false;
        }

        return ServiceBusMetadataJson.ObjectDeserializer.TryGetValue(AsNonNullableDictionary(dictionary), key, out value);
    }

    static IDictionary<string, object> AsNonNullableDictionary(IDictionary<string, object?> dictionary) =>
        (IDictionary<string, object>)(object)dictionary;
}
