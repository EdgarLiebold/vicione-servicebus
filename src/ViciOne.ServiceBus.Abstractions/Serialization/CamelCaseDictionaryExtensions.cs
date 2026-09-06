using System.Collections.Generic;

namespace ViciOne.ServiceBus.Serialization;

/// <summary>Provides extension methods for camel case dictionary.</summary>
public static class CamelCaseDictionaryExtensions
{
    /// <summary>Converts a PascalCase key to camelCase and attempts to get the value from the dictionary.</summary>
    /// <param name="dictionary">The dictionary.</param>
    /// <param name="key">The key used to identify the requested entry.</param>
    /// <param name="value">Receives the value produced by the operation.</param>
    /// <returns><see langword="true" /> when the condition is satisfied; otherwise, <see langword="false" />.</returns>
    public static bool TryGetValueCamelCase(this IDictionary<string, object>? dictionary, string key, out object? value)
    {
        if (dictionary != null && char.IsUpper(key[0]))
        {
            var chars = key.ToCharArray();
            chars[0] = char.ToLower(chars[0]);

            key = new string(chars);
            return dictionary.TryGetValue(key, out value);
        }

        value = null;
        return false;
    }

    /// <summary>Converts a PascalCase key to camelCase and attempts to get the value from the dictionary.</summary>
    /// <param name="dictionary">The dictionary.</param>
    /// <param name="key">The key used to identify the requested entry.</param>
    /// <param name="value">Receives the value produced by the operation.</param>
    /// <returns><see langword="true" /> when the condition is satisfied; otherwise, <see langword="false" />.</returns>
    public static bool TryGetValueCamelCase(this IReadOnlyDictionary<string, object>? dictionary, string key, out object? value)
    {
        if (dictionary != null && char.IsUpper(key[0]))
        {
            var chars = key.ToCharArray();
            chars[0] = char.ToLower(chars[0]);

            key = new string(chars);
            return dictionary.TryGetValue(key, out value);
        }

        value = null;
        return false;
    }
}
