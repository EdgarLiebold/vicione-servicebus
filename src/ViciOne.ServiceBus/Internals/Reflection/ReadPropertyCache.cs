using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;

namespace ViciOne.ServiceBus.Internals.Reflection;

internal sealed class ReadPropertyCache<T>
    where T : class
{
    readonly IDictionary<string, IReadProperty<T>> _properties;
    readonly IReadOnlyDictionary<string, PropertyInfo> _propertyIndex;

    ReadPropertyCache()
    {
        _properties = new Dictionary<string, IReadProperty<T>>(StringComparer.OrdinalIgnoreCase);
        _propertyIndex = MessageTypeCache<T>.Properties.ToDictionary(x => x.Name, StringComparer.OrdinalIgnoreCase);
    }

    IReadProperty<T, TProperty> GetRequiredProperty<TProperty>(string name)
    {
        name = RequirePropertyName(name);
        if (!_propertyIndex.TryGetValue(name, out PropertyInfo? propertyInfo))
            throw new ArgumentException($"{TypeCache<T>.ShortName} does not contain the property: {name}", nameof(name));

        if (propertyInfo.PropertyType != typeof(TProperty))
            throw PropertyTypeMismatch<TProperty>(name, propertyInfo);

        return GetOrAddProperty<TProperty>(name, propertyInfo);
    }

    bool TryGetPropertyCore<TProperty>(string name, [NotNullWhen(true)] out IReadProperty<T, TProperty>? property)
    {
        name = RequirePropertyName(name);
        if (!_propertyIndex.TryGetValue(name, out PropertyInfo? propertyInfo)
            || propertyInfo.PropertyType != typeof(TProperty))
        {
            property = null;
            return false;
        }

        property = GetOrAddProperty<TProperty>(name, propertyInfo);
        return true;
    }

    IReadProperty<T, TProperty> GetOrAddProperty<TProperty>(string name, PropertyInfo propertyInfo)
    {
        lock (_properties)
        {
            if (_properties.TryGetValue(name, out IReadProperty<T>? property))
                return (IReadProperty<T, TProperty>)property;

            var readProperty = new ReadProperty<T, TProperty>(propertyInfo);
            _properties.Add(name, readProperty);
            return readProperty;
        }
    }

    internal static IReadProperty<T, TProperty> GetProperty<TProperty>(string name)
    {
        return Cached.PropertyCache.Value.GetRequiredProperty<TProperty>(name);
    }

    internal static bool TryGetProperty<TProperty>(
        string name,
        [NotNullWhen(true)] out IReadProperty<T, TProperty>? property)
    {
        return Cached.PropertyCache.Value.TryGetPropertyCore(name, out property);
    }

    internal static IReadProperty<T, TProperty> GetProperty<TProperty>(PropertyInfo? propertyInfo)
    {
        return Cached.PropertyCache.Value.GetRequiredProperty<TProperty>(RequireOwnedPropertyName(propertyInfo));
    }

    static string RequirePropertyName(string? name)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        return name;
    }

    static string RequireOwnedPropertyName(PropertyInfo? propertyInfo)
    {
        ArgumentNullException.ThrowIfNull(propertyInfo);
        if (propertyInfo.DeclaringType == null || !propertyInfo.DeclaringType.IsAssignableFrom(typeof(T)))
            throw new ArgumentException($"Property {propertyInfo.Name} cannot be read from {typeof(T)}.", nameof(propertyInfo));

        return propertyInfo.Name;
    }

    static ArgumentException PropertyTypeMismatch<TProperty>(string name, PropertyInfo propertyInfo)
    {
        return new ArgumentException(
            $"Property {name} on {TypeCache<T>.ShortName} has type {TypeCache.GetShortName(propertyInfo.PropertyType)}, not {TypeCache<TProperty>.ShortName}.",
            nameof(name));
    }

    static class Cached
    {
        internal static readonly Lazy<ReadPropertyCache<T>> PropertyCache = new(() => new ReadPropertyCache<T>());
    }
}
