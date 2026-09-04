using System;
using System.Linq.Expressions;
using System.Reflection;
using ViciOne.ServiceBus.Internals;

namespace ViciOne.ServiceBus.Metadata;

public class ReadOnlyProperty
{
    public readonly Func<object, object?> GetProperty;

    public ReadOnlyProperty(PropertyInfo property, PropertyAccessPolicy accessPolicy = PropertyAccessPolicy.PublicOnly)
    {
        Property = PropertyAccessorFactory.Validate(property);
        GetProperty = PropertyAccessorFactory.CreateUntypedGetter(Property, accessPolicy);
    }

    public PropertyInfo Property { get; }

    public object? Get(object instance) => GetProperty(instance);
}


public class ReadOnlyProperty<T>
{
    public readonly Func<T, object?> GetProperty;

    public ReadOnlyProperty(Expression<Func<T, object>> propertyExpression, PropertyAccessPolicy accessPolicy = PropertyAccessPolicy.PublicOnly)
        : this(propertyExpression.GetPropertyInfo(), accessPolicy)
    {
    }

    public ReadOnlyProperty(PropertyInfo? property, PropertyAccessPolicy accessPolicy = PropertyAccessPolicy.PublicOnly)
    {
        Property = PropertyAccessorFactory.Validate(property, typeof(T));
        GetProperty = PropertyAccessorFactory.CreateGetter<T>(Property, accessPolicy);
    }

    public PropertyInfo Property { get; }

    public object? Get(T instance) => GetProperty(instance);
}


public class ReadOnlyProperty<T, TProperty>
{
    public readonly Func<T, TProperty> GetProperty;

    public ReadOnlyProperty(Expression<Func<T, object>> propertyExpression, PropertyAccessPolicy accessPolicy = PropertyAccessPolicy.PublicOnly)
        : this(propertyExpression.GetPropertyInfo(), accessPolicy)
    {
    }

    public ReadOnlyProperty(PropertyInfo? property, PropertyAccessPolicy accessPolicy = PropertyAccessPolicy.PublicOnly)
    {
        Property = PropertyAccessorFactory.Validate(property, typeof(T), typeof(TProperty));
        GetProperty = PropertyAccessorFactory.CreateGetter<T, TProperty>(Property, accessPolicy);
    }

    public PropertyInfo Property { get; }

    public TProperty Get(T instance) => GetProperty(instance);
}
