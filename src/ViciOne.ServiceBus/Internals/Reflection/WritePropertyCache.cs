using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace ViciOne.ServiceBus.Internals.Reflection;

internal sealed class WritePropertyCache<T>
    where T : class
{
    readonly Type _implementationType;
    readonly IDictionary<string, IWriteProperty<T>> _properties;
    readonly IReadOnlyDictionary<string, PropertyInfo> _propertyIndex;

    WritePropertyCache()
    {
        if (MessageTypeCache<T>.IsValidMessageType && typeof(T).IsInterface)
        {
            _implementationType = MessageImplementationCache<T>.ImplementationType;
            _propertyIndex = _implementationType.GetReadableInstanceProperties()
                .GroupBy(x => x.Name)
                .Select(x => x.Last())
                .ToDictionary(x => x.Name, StringComparer.OrdinalIgnoreCase);
        }
        else
        {
            _implementationType = typeof(T);
            _propertyIndex = MessageTypeCache<T>.Properties
                .ToDictionary(x => x.Name, StringComparer.OrdinalIgnoreCase);
        }

        _properties = new Dictionary<string, IWriteProperty<T>>(StringComparer.OrdinalIgnoreCase);
    }

    bool CanWriteCore(string name)
    {
        name = RequirePropertyName(name);
        if (_propertyIndex.TryGetValue(name, out PropertyInfo? propertyInfo))
            return propertyInfo.CanWrite;

        throw new ArgumentException($"{TypeCache<T>.ShortName} does not contain the property: {name}", nameof(name));
    }

    IWriteProperty<T, TProperty> GetRequiredProperty<TProperty>(string name)
    {
        name = RequirePropertyName(name);
        if (!_propertyIndex.TryGetValue(name, out PropertyInfo? propertyInfo))
            throw new ArgumentException($"{TypeCache<T>.ShortName} does not contain the property: {name}", nameof(name));

        if (propertyInfo.PropertyType != typeof(TProperty))
            throw PropertyTypeMismatch<TProperty>(name, propertyInfo);

        lock (_properties)
        {
            if (_properties.TryGetValue(name, out IWriteProperty<T>? property))
                return (IWriteProperty<T, TProperty>)property;

            var writeProperty = new WriteProperty<T, TProperty>(_implementationType, propertyInfo);
            _properties.Add(name, writeProperty);
            return writeProperty;
        }
    }

    internal static IWriteProperty<T, TProperty> GetProperty<TProperty>(string name)
    {
        return Cached.PropertyCache.Value.GetRequiredProperty<TProperty>(name);
    }

    internal static IWriteProperty<T, TProperty> GetProperty<TProperty>(PropertyInfo? propertyInfo)
    {
        return Cached.PropertyCache.Value.GetRequiredProperty<TProperty>(RequireOwnedPropertyName(propertyInfo));
    }

    internal static bool CanWrite(string name)
    {
        return Cached.PropertyCache.Value.CanWriteCore(name);
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
            throw new ArgumentException($"Property {propertyInfo.Name} cannot be written on {typeof(T)}.", nameof(propertyInfo));

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
        internal static readonly Lazy<WritePropertyCache<T>> PropertyCache = new(() => new WritePropertyCache<T>());
    }
}
