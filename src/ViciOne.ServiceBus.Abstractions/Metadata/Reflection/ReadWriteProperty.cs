using System;
using System.Linq.Expressions;
using System.Reflection;
using ViciOne.ServiceBus.Internals;

namespace ViciOne.ServiceBus.Metadata;

public class ReadWriteProperty : ReadOnlyProperty
{
    public readonly Action<object, object?> SetProperty;

    public ReadWriteProperty(PropertyInfo property, PropertyAccessPolicy accessPolicy = PropertyAccessPolicy.PublicOnly)
        : base(property, accessPolicy)
    {
        SetProperty = PropertyAccessorFactory.CreateUntypedSetter(Property, accessPolicy);
    }

    public void Set(object instance, object? value) => SetProperty(instance, value);
}


public class ReadWriteProperty<T> : ReadOnlyProperty<T>
{
    public readonly Action<T, object?> SetProperty;

    public ReadWriteProperty(Expression<Func<T, object>> propertyExpression, PropertyAccessPolicy accessPolicy = PropertyAccessPolicy.PublicOnly)
        : this(propertyExpression.GetPropertyInfo(), accessPolicy)
    {
    }

    public ReadWriteProperty(PropertyInfo? property, PropertyAccessPolicy accessPolicy = PropertyAccessPolicy.PublicOnly)
        : base(property, accessPolicy)
    {
        SetProperty = PropertyAccessorFactory.CreateSetter<T>(Property, accessPolicy);
    }

    public void Set(T instance, object? value) => SetProperty(instance, value);
}


public class ReadWriteProperty<T, TProperty> : ReadOnlyProperty<T, TProperty>
{
    public readonly Action<T, TProperty> SetProperty;

    public ReadWriteProperty(Expression<Func<T, object>> propertyExpression, PropertyAccessPolicy accessPolicy = PropertyAccessPolicy.PublicOnly)
        : this(propertyExpression.GetPropertyInfo(), accessPolicy)
    {
    }

    public ReadWriteProperty(PropertyInfo? property, PropertyAccessPolicy accessPolicy = PropertyAccessPolicy.PublicOnly)
        : base(property, accessPolicy)
    {
        SetProperty = PropertyAccessorFactory.CreateSetter<T, TProperty>(Property, accessPolicy);
    }

    public void Set(T instance, TProperty value) => SetProperty(instance, value);
}
