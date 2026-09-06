using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace ViciOne.ServiceBus.Metadata;

/// <summary>Provides cached access to read write property data.</summary>
/// <typeparam name="T">The value type.</typeparam>
public interface IReadWritePropertyCache<T> : IEnumerable<ReadWriteProperty<T>>
{
    /// <summary>Gets or sets the value at the specified index.</summary>
    /// <param name="name">The name.</param>
    ReadWriteProperty<T> this[string name] { get; }

    /// <summary>Attempts to get value.</summary>
    /// <param name="key">The key used to identify the requested entry.</param>
    /// <param name="value">Receives the value produced by the operation.</param>
    /// <returns><see langword="true" /> when the condition is satisfied; otherwise, <see langword="false" />.</returns>
    bool TryGetValue(string key, [NotNullWhen(true)] out ReadWriteProperty<T>? value);

    /// <summary>Attempts to get property.</summary>
    /// <param name="propertyName">The property name.</param>
    /// <param name="property">Receives the property produced by the operation.</param>
    /// <returns><see langword="true" /> when the condition is satisfied; otherwise, <see langword="false" />.</returns>
    bool TryGetProperty(string propertyName, [NotNullWhen(true)] out ReadWriteProperty<T>? property);
}
