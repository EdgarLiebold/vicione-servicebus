using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Linq.Expressions;
using ViciOne.ServiceBus.Internals;

namespace ViciOne.ServiceBus.Metadata;

/// <summary>Caches read write property data.</summary>
/// <typeparam name="T">The value type.</typeparam>
public class ReadWritePropertyCache<T> : IReadWritePropertyCache<T>
{
    readonly IReadOnlyDictionary<string, ReadWriteProperty<T>> _properties;

    /// <summary>Initializes a new instance.</summary>
    /// <param name="accessPolicy">The access policy.</param>
    public ReadWritePropertyCache(PropertyAccessPolicy accessPolicy = PropertyAccessPolicy.PublicOnly)
    {
        _properties = CreatePropertyCache(accessPolicy);
    }

    /// <summary>Gets or sets the value at the specified index.</summary>
    /// <param name="name">The name.</param>
    public ReadWriteProperty<T> this[string name] => _properties[name];

    /// <summary>Gets enumerator.</summary>
    /// <returns>The enumerator.</returns>
    public IEnumerator<ReadWriteProperty<T>> GetEnumerator() => _properties.Values.GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    /// <summary>Attempts to get value.</summary>
    /// <param name="key">The key used to identify the requested entry.</param>
    /// <param name="value">Receives the value produced by the operation.</param>
    /// <returns><see langword="true" /> when the condition is satisfied; otherwise, <see langword="false" />.</returns>
    public bool TryGetValue(string key, [NotNullWhen(true)] out ReadWriteProperty<T>? value) => _properties.TryGetValue(key, out value);

    /// <summary>Attempts to get property.</summary>
    /// <param name="propertyName">The property name.</param>
    /// <param name="property">Receives the property produced by the operation.</param>
    /// <returns><see langword="true" /> when the condition is satisfied; otherwise, <see langword="false" />.</returns>
    public bool TryGetProperty(string propertyName, [NotNullWhen(true)] out ReadWriteProperty<T>? property) =>
        _properties.TryGetValue(propertyName, out property);

    static IReadOnlyDictionary<string, ReadWriteProperty<T>> CreatePropertyCache(PropertyAccessPolicy accessPolicy)
    {
        bool includeNonPublic = PropertyAccessorFactory.IncludeNonPublic(accessPolicy);

        return typeof(T).GetReadableInstanceProperties()
            .Where(property => property.GetGetMethod(includeNonPublic) != null && property.GetSetMethod(includeNonPublic) != null)
            .GroupBy(property => property.Name, StringComparer.OrdinalIgnoreCase)
            .Select(group => group.Last())
            .Select(property => new ReadWriteProperty<T>(property, accessPolicy))
            .ToDictionary(property => property.Property.Name, StringComparer.OrdinalIgnoreCase);
    }

    /// <summary>Updates the target with the supplied value.</summary>
    /// <param name="propertyExpression">The property expression.</param>
    /// <param name="instance">The instance.</param>
    /// <param name="value">The value to process.</param>
    public void Set(Expression<Func<T, object>> propertyExpression, T instance, object? value) =>
        _properties[propertyExpression.GetMemberName()].Set(instance, value);

    /// <summary>Retrieves the requested value.</summary>
    /// <param name="propertyExpression">The property expression.</param>
    /// <param name="instance">The instance.</param>
    /// <returns>The requested value.</returns>
    public object? Get(Expression<Func<T, object>> propertyExpression, T instance) =>
        _properties[propertyExpression.GetMemberName()].Get(instance);
}
