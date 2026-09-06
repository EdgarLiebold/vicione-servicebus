using System;
using System.Linq.Expressions;
using System.Reflection;
using ViciOne.ServiceBus.Internals;

namespace ViciOne.ServiceBus.Metadata;

/// <summary>Provides access to the read write property.</summary>
public class ReadWriteProperty : ReadOnlyProperty
{
    /// <summary>Exposes the set property used by the containing type.</summary>
    public readonly Action<object, object?> SetProperty;

    /// <summary>Initializes a new instance.</summary>
    /// <param name="property">The property.</param>
    /// <param name="accessPolicy">The access policy.</param>
    public ReadWriteProperty(PropertyInfo property, PropertyAccessPolicy accessPolicy = PropertyAccessPolicy.PublicOnly)
        : base(property, accessPolicy)
    {
        SetProperty = PropertyAccessorFactory.CreateUntypedSetter(Property, accessPolicy);
    }

    /// <summary>Updates the target with the supplied value.</summary>
    /// <param name="instance">The instance.</param>
    /// <param name="value">The value to process.</param>
    public void Set(object instance, object? value) => SetProperty(instance, value);
}


/// <summary>Provides access to the read write property.</summary>
/// <typeparam name="T">The value type.</typeparam>
public class ReadWriteProperty<T> : ReadOnlyProperty<T>
{
    /// <summary>Exposes the set property used by the containing type.</summary>
    public readonly Action<T, object?> SetProperty;

    /// <summary>Initializes a new instance.</summary>
    /// <param name="propertyExpression">The property expression.</param>
    /// <param name="accessPolicy">The access policy.</param>
    public ReadWriteProperty(Expression<Func<T, object>> propertyExpression, PropertyAccessPolicy accessPolicy = PropertyAccessPolicy.PublicOnly)
        : this(propertyExpression.GetPropertyInfo(), accessPolicy)
    {
    }

    /// <summary>Initializes a new instance.</summary>
    /// <param name="property">The property.</param>
    /// <param name="accessPolicy">The access policy.</param>
    public ReadWriteProperty(PropertyInfo? property, PropertyAccessPolicy accessPolicy = PropertyAccessPolicy.PublicOnly)
        : base(property, accessPolicy)
    {
        SetProperty = PropertyAccessorFactory.CreateSetter<T>(Property, accessPolicy);
    }

    /// <summary>Updates the target with the supplied value.</summary>
    /// <param name="instance">The instance.</param>
    /// <param name="value">The value to process.</param>
    public void Set(T instance, object? value) => SetProperty(instance, value);
}


/// <summary>Provides access to the read write property.</summary>
/// <typeparam name="T">The value type.</typeparam>
/// <typeparam name="TProperty">The property type.</typeparam>
public class ReadWriteProperty<T, TProperty> : ReadOnlyProperty<T, TProperty>
{
    /// <summary>Exposes the set property used by the containing type.</summary>
    public readonly Action<T, TProperty> SetProperty;

    /// <summary>Initializes a new instance.</summary>
    /// <param name="propertyExpression">The property expression.</param>
    /// <param name="accessPolicy">The access policy.</param>
    public ReadWriteProperty(Expression<Func<T, object>> propertyExpression, PropertyAccessPolicy accessPolicy = PropertyAccessPolicy.PublicOnly)
        : this(propertyExpression.GetPropertyInfo(), accessPolicy)
    {
    }

    /// <summary>Initializes a new instance.</summary>
    /// <param name="property">The property.</param>
    /// <param name="accessPolicy">The access policy.</param>
    public ReadWriteProperty(PropertyInfo? property, PropertyAccessPolicy accessPolicy = PropertyAccessPolicy.PublicOnly)
        : base(property, accessPolicy)
    {
        SetProperty = PropertyAccessorFactory.CreateSetter<T, TProperty>(Property, accessPolicy);
    }

    /// <summary>Updates the target with the supplied value.</summary>
    /// <param name="instance">The instance.</param>
    /// <param name="value">The value to process.</param>
    public void Set(T instance, TProperty value) => SetProperty(instance, value);
}
