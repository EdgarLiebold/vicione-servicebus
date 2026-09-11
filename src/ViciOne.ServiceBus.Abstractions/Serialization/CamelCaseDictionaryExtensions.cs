using System.Collections.Generic;
using System.Text.Json;

namespace ViciOne.ServiceBus.Serialization;

/// <summary>Provides PascalCase-to-camelCase fallback lookup for metadata dictionaries.</summary>
internal static class CamelCaseDictionaryExtensions
{
    /// <summary>Tries to find a mutable dictionary entry under the JSON camel-case form of a PascalCase key.</summary>
    /// <param name="dictionary">The dictionary to search, or <see langword="null" /> to report absence.</param>
    /// <param name="key">The PascalCase metadata key to normalize.</param>
    /// <param name="value">The value stored under the normalized key when found; otherwise, <see langword="null" />.</param>
    /// <returns><see langword="true" /> when the key begins with an uppercase character and its camel-case form exists; otherwise, <see langword="false" />.</returns>
    internal static bool TryGetValueCamelCase(this IDictionary<string, object>? dictionary, string key, out object? value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);

        if (dictionary != null && char.IsUpper(key[0]))
        {
            key = JsonNamingPolicy.CamelCase.ConvertName(key);
            return dictionary.TryGetValue(key, out value);
        }

        value = null;
        return false;
    }

    /// <summary>Tries to find a read-only dictionary entry under the JSON camel-case form of a PascalCase key.</summary>
    /// <param name="dictionary">The dictionary to search, or <see langword="null" /> to report absence.</param>
    /// <param name="key">The PascalCase metadata key to normalize.</param>
    /// <param name="value">The value stored under the normalized key when found; otherwise, <see langword="null" />.</param>
    /// <returns><see langword="true" /> when the key begins with an uppercase character and its camel-case form exists; otherwise, <see langword="false" />.</returns>
    internal static bool TryGetValueCamelCase(this IReadOnlyDictionary<string, object>? dictionary, string key, out object? value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);

        if (dictionary != null && char.IsUpper(key[0]))
        {
            key = JsonNamingPolicy.CamelCase.ConvertName(key);
            return dictionary.TryGetValue(key, out value);
        }

        value = null;
        return false;
    }
}
