using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Linq.Expressions;
using ViciOne.ServiceBus.Internals;

namespace ViciOne.ServiceBus.Metadata;

/// <summary>
/// Provides a read write property cache implementation.
/// </summary>
/// <typeparam name="T">The t type.</typeparam>
public class ReadWritePropertyCache<T> : IReadWritePropertyCache<T>
{
    readonly IReadOnlyDictionary<string, ReadWriteProperty<T>> _properties;

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="accessPolicy">The access policy value.</param>
    public ReadWritePropertyCache(PropertyAccessPolicy accessPolicy = PropertyAccessPolicy.PublicOnly)
    {
        _properties = CreatePropertyCache(accessPolicy);
    }

    /// <summary>
    /// Gets or sets the value at the specified index.
    /// </summary>
    /// <param name="name">The name value.</param>
    public ReadWriteProperty<T> this[string name] => _properties[name];

    /// <summary>
    /// Gets enumerator.
    /// </summary>
    /// <returns>The result of the operation.</returns>
    public IEnumerator<ReadWriteProperty<T>> GetEnumerator() => _properties.Values.GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    /// <summary>
    /// Attempts to get value.
    /// </summary>
    /// <param name="key">The key value.</param>
    /// <param name="value">The value.</param>
    /// <returns><see langword="true" /> when the condition is satisfied; otherwise, <see langword="false" />.</returns>
    public bool TryGetValue(string key, [NotNullWhen(true)] out ReadWriteProperty<T>? value) => _properties.TryGetValue(key, out value);

    /// <summary>
    /// Attempts to get property.
    /// </summary>
    /// <param name="propertyName">The property name value.</param>
    /// <param name="property">The property value.</param>
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

    /// <summary>
    /// Performs the set operation.
    /// </summary>
    /// <param name="propertyExpression">The property expression value.</param>
    /// <param name="instance">The instance value.</param>
    /// <param name="value">The value.</param>
    public void Set(Expression<Func<T, object>> propertyExpression, T instance, object? value) =>
        _properties[propertyExpression.GetMemberName()].Set(instance, value);

    /// <summary>
    /// Performs the get operation.
    /// </summary>
    /// <param name="propertyExpression">The property expression value.</param>
    /// <param name="instance">The instance value.</param>
    /// <returns>The result of the operation.</returns>
    public object? Get(Expression<Func<T, object>> propertyExpression, T instance) =>
        _properties[propertyExpression.GetMemberName()].Get(instance);
}
