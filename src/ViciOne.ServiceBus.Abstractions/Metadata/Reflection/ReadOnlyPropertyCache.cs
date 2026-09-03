namespace ViciOne.ServiceBus.Metadata;

using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Linq.Expressions;
using Internals;


public class ReadOnlyPropertyCache<T> : IReadOnlyPropertyCache<T>
{
    readonly IReadOnlyDictionary<string, ReadOnlyProperty<T>> _properties;

    public ReadOnlyPropertyCache(PropertyAccessPolicy accessPolicy = PropertyAccessPolicy.PublicOnly)
    {
        _properties = CreatePropertyCache(accessPolicy);
    }

    public IEnumerator<ReadOnlyProperty<T>> GetEnumerator() => _properties.Values.GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

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

    public object? Get(Expression<Func<T, object>> propertyExpression, T instance) =>
        _properties[propertyExpression.GetMemberName()].Get(instance);
}
