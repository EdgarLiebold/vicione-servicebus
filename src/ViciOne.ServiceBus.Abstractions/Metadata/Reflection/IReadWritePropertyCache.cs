using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace ViciOne.ServiceBus.Metadata;

/// <summary>
/// Defines the contract for read write property cache.
/// </summary>
/// <typeparam name="T">The t type.</typeparam>
public interface IReadWritePropertyCache<T> : IEnumerable<ReadWriteProperty<T>>
{
    /// <summary>
    /// Gets or sets the value at the specified index.
    /// </summary>
    /// <param name="name">The name value.</param>
    ReadWriteProperty<T> this[string name] { get; }

    /// <summary>
    /// Attempts to get value.
    /// </summary>
    /// <param name="key">The key value.</param>
    /// <param name="value">The value.</param>
    /// <returns><see langword="true" /> when the condition is satisfied; otherwise, <see langword="false" />.</returns>
    bool TryGetValue(string key, [NotNullWhen(true)] out ReadWriteProperty<T>? value);

    /// <summary>
    /// Attempts to get property.
    /// </summary>
    /// <param name="propertyName">The property name value.</param>
    /// <param name="property">The property value.</param>
    /// <returns><see langword="true" /> when the condition is satisfied; otherwise, <see langword="false" />.</returns>
    bool TryGetProperty(string propertyName, [NotNullWhen(true)] out ReadWriteProperty<T>? property);
}
