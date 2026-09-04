using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Linq.Expressions;
using ViciOne.ServiceBus.Internals;

namespace ViciOne.ServiceBus.Metadata;

/// <summary>
/// Provides a read only property cache implementation.
/// </summary>
/// <typeparam name="T">The t type.</typeparam>
public class ReadOnlyPropertyCache<T> : IReadOnlyPropertyCache<T>
{
    readonly IReadOnlyDictionary<string, ReadOnlyProperty<T>> _properties;

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="accessPolicy">The access policy value.</param>
    public ReadOnlyPropertyCache(PropertyAccessPolicy accessPolicy = PropertyAccessPolicy.PublicOnly)
    {
        _properties = CreatePropertyCache(accessPolicy);
    }

    /// <summary>
    /// Gets enumerator.
    /// </summary>
    /// <returns>The result of the operation.</returns>
    public IEnumerator<ReadOnlyProperty<T>> GetEnumerator() => _properties.Values.GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    /// <summary>
    /// Attempts to get value.
    /// </summary>
    /// <param name="key">The key value.</param>
    /// <param name="value">The value.</param>
    /// <returns><see langword="true" /> when the condition is satisfied; otherwise, <see langword="false" />.</returns>
    public bool TryGetValue(string key, [NotNullWhen(true)] out ReadOnlyProperty<T>? value) => _properties.TryGetValue(key, out value);

    static IReadOnlyDictionary<string, ReadOnlyProperty<T>> CreatePropertyCache(PropertyAccessPolicy accessPolicy)
    {
        bool includeNonPublic = PropertyAccessorFactory.IncludeNonPublic(accessPolicy);

        return typeof(T).GetReadableInstanceProperties()
            .Where(property => property.GetGetMethod(includeNonPublic) != null)
            .GroupBy(property => property.Name, StringComparer.OrdinalIgnoreCase)
            .Select(group => group.Last())
            .Select(property => new ReadOnlyProperty<T>(property, accessPolicy))
            .ToDictionary(property => property.Property.Name, StringComparer.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Performs the get operation.
    /// </summary>
    /// <param name="propertyExpression">The property expression value.</param>
    /// <param name="instance">The instance value.</param>
    /// <returns>The result of the operation.</returns>
    public object? Get(Expression<Func<T, object>> propertyExpression, T instance) =>
        _properties[propertyExpression.GetMemberName()].Get(instance);
}
