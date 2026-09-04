using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Linq.Expressions;
using ViciOne.ServiceBus.Internals;

namespace ViciOne.ServiceBus.Metadata;

public class ReadWritePropertyCache<T> : IReadWritePropertyCache<T>
{
    readonly IReadOnlyDictionary<string, ReadWriteProperty<T>> _properties;

    public ReadWritePropertyCache(PropertyAccessPolicy accessPolicy = PropertyAccessPolicy.PublicOnly)
    {
        _properties = CreatePropertyCache(accessPolicy);
    }

    public ReadWriteProperty<T> this[string name] => _properties[name];

    public IEnumerator<ReadWriteProperty<T>> GetEnumerator() => _properties.Values.GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    public bool TryGetValue(string key, [NotNullWhen(true)] out ReadWriteProperty<T>? value) => _properties.TryGetValue(key, out value);

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

    public void Set(Expression<Func<T, object>> propertyExpression, T instance, object? value) =>
        _properties[propertyExpression.GetMemberName()].Set(instance, value);

    public object? Get(Expression<Func<T, object>> propertyExpression, T instance) =>
        _properties[propertyExpression.GetMemberName()].Get(instance);
}
