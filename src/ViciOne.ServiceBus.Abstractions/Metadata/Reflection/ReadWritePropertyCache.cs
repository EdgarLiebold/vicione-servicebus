using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Linq.Expressions;
using ViciOne.ServiceBus.Internals;

namespace ViciOne.ServiceBus.Metadata;

/// <summary>Caches readable and writable instance properties for case-insensitive lookup.</summary>
/// <typeparam name="T">The target type.</typeparam>
public class ReadWritePropertyCache<T> : IReadWritePropertyCache<T>
{
    readonly IReadOnlyDictionary<string, ReadWriteProperty<T>> _properties;

    /// <summary>Builds the property cache for <typeparamref name="T" />.</summary>
    /// <param name="accessPolicy">The accessibility boundary applied to property getters and setters.</param>
    public ReadWritePropertyCache(PropertyAccessPolicy accessPolicy = PropertyAccessPolicy.PublicOnly)
    {
        _properties = CreatePropertyCache(accessPolicy);
    }

    /// <summary>Gets a cached property by name.</summary>
    /// <param name="name">The case-insensitive property name.</param>
    public ReadWriteProperty<T> this[string name] => _properties[name];

    /// <summary>Returns an enumerator over the cached properties.</summary>
    /// <returns>An enumerator over the cached property accessors.</returns>
    public IEnumerator<ReadWriteProperty<T>> GetEnumerator() => _properties.Values.GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    /// <summary>Attempts to get a cached property by name.</summary>
    /// <param name="key">The case-insensitive property name.</param>
    /// <param name="value">Receives the cached property when found.</param>
    /// <returns><see langword="true" /> when the property exists.</returns>
    public bool TryGetValue(string key, [NotNullWhen(true)] out ReadWriteProperty<T>? value) => _properties.TryGetValue(key, out value);

    static IReadOnlyDictionary<string, ReadWriteProperty<T>> CreatePropertyCache(PropertyAccessPolicy accessPolicy)
    {
        bool includeNonPublic = PropertyAccessorFactory.IncludeNonPublic(accessPolicy);

        return typeof(T).GetReadableInstanceProperties()
            .Where(property => property.GetIndexParameters().Length == 0
                && property.GetGetMethod(includeNonPublic) != null && property.GetSetMethod(includeNonPublic) != null)
            .GroupBy(property => property.Name, StringComparer.OrdinalIgnoreCase)
            .Select(group => group.Last())
            .Select(property => new ReadWriteProperty<T>(property, accessPolicy))
            .ToDictionary(property => property.Property.Name, StringComparer.OrdinalIgnoreCase);
    }

    /// <summary>Writes a selected property on an instance.</summary>
    /// <param name="propertyExpression">An expression selecting a property on <typeparamref name="T" />.</param>
    /// <param name="instance">The target instance.</param>
    /// <param name="value">The new property value.</param>
    public void Set(Expression<Func<T, object>> propertyExpression, T instance, object? value) =>
        _properties[propertyExpression.GetMemberName()].Set(instance, value);

    /// <summary>Reads a selected property from an instance.</summary>
    /// <param name="propertyExpression">An expression selecting a property on <typeparamref name="T" />.</param>
    /// <param name="instance">The target instance.</param>
    /// <returns>The boxed property value.</returns>
    public object? Get(Expression<Func<T, object>> propertyExpression, T instance) =>
        _properties[propertyExpression.GetMemberName()].Get(instance);
}
