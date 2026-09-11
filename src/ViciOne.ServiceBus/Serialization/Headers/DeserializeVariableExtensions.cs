using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace ViciOne.ServiceBus.Serialization;

/// <summary>Reads typed values from nullable metadata dictionaries.</summary>
public static class DeserializeVariableExtensions
{
    /// <summary>Tries to convert a dictionary entry to a reference type with the stable metadata codec.</summary>
    /// <typeparam name="T">The requested reference type.</typeparam>
    /// <param name="dictionary">The metadata dictionary, or <see langword="null" />.</param>
    /// <param name="key">The non-empty entry name.</param>
    /// <param name="value">The converted value when present and compatible.</param>
    /// <returns><see langword="true" /> when conversion produces a non-null value; otherwise, <see langword="false" />.</returns>
    public static bool TryGetValue<T>(this IDictionary<string, object?>? dictionary, string key, [NotNullWhen(true)] out T? value)
        where T : class
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);
        if (dictionary is null)
        {
            value = null;
            return false;
        }

        return ServiceBusMetadataJson.ObjectDeserializer.TryGetValue(AsNonNullableDictionary(dictionary), key, out value);
    }

    /// <summary>Tries to convert a dictionary entry to a value type with the stable metadata codec.</summary>
    /// <typeparam name="T">The requested value type.</typeparam>
    /// <param name="dictionary">The metadata dictionary, or <see langword="null" />.</param>
    /// <param name="key">The non-empty entry name.</param>
    /// <param name="value">The converted value when present and compatible.</param>
    /// <returns><see langword="true" /> when conversion produces a value; otherwise, <see langword="false" />.</returns>
    public static bool TryGetValue<T>(this IDictionary<string, object?>? dictionary, string key, [NotNullWhen(true)] out T? value)
        where T : struct
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);
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
