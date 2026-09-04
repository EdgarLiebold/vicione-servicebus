using System;
using System.Linq.Expressions;
using System.Reflection;
using ViciOne.ServiceBus.Internals;

namespace ViciOne.ServiceBus.Metadata;

/// <summary>
/// Provides a read write property implementation.
/// </summary>
public class ReadWriteProperty : ReadOnlyProperty
{
    /// <summary>
    /// Defines the set property value.
    /// </summary>
    public readonly Action<object, object?> SetProperty;

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="property">The property value.</param>
    /// <param name="accessPolicy">The access policy value.</param>
    public ReadWriteProperty(PropertyInfo property, PropertyAccessPolicy accessPolicy = PropertyAccessPolicy.PublicOnly)
        : base(property, accessPolicy)
    {
        SetProperty = PropertyAccessorFactory.CreateUntypedSetter(Property, accessPolicy);
    }

    /// <summary>
    /// Performs the set operation.
    /// </summary>
    /// <param name="instance">The instance value.</param>
    /// <param name="value">The value.</param>
    public void Set(object instance, object? value) => SetProperty(instance, value);
}


/// <summary>
/// Provides a read write property implementation.
/// </summary>
/// <typeparam name="T">The t type.</typeparam>
public class ReadWriteProperty<T> : ReadOnlyProperty<T>
{
    /// <summary>
    /// Defines the set property value.
    /// </summary>
    public readonly Action<T, object?> SetProperty;

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="propertyExpression">The property expression value.</param>
    /// <param name="accessPolicy">The access policy value.</param>
    public ReadWriteProperty(Expression<Func<T, object>> propertyExpression, PropertyAccessPolicy accessPolicy = PropertyAccessPolicy.PublicOnly)
        : this(propertyExpression.GetPropertyInfo(), accessPolicy)
    {
    }

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="property">The property value.</param>
    /// <param name="accessPolicy">The access policy value.</param>
    public ReadWriteProperty(PropertyInfo? property, PropertyAccessPolicy accessPolicy = PropertyAccessPolicy.PublicOnly)
        : base(property, accessPolicy)
    {
        SetProperty = PropertyAccessorFactory.CreateSetter<T>(Property, accessPolicy);
    }

    /// <summary>
    /// Performs the set operation.
    /// </summary>
    /// <param name="instance">The instance value.</param>
    /// <param name="value">The value.</param>
    public void Set(T instance, object? value) => SetProperty(instance, value);
}


/// <summary>
/// Provides a read write property implementation.
/// </summary>
/// <typeparam name="T">The t type.</typeparam>
/// <typeparam name="TProperty">The t property type.</typeparam>
public class ReadWriteProperty<T, TProperty> : ReadOnlyProperty<T, TProperty>
{
    /// <summary>
    /// Defines the set property value.
    /// </summary>
    public readonly Action<T, TProperty> SetProperty;

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="propertyExpression">The property expression value.</param>
    /// <param name="accessPolicy">The access policy value.</param>
    public ReadWriteProperty(Expression<Func<T, object>> propertyExpression, PropertyAccessPolicy accessPolicy = PropertyAccessPolicy.PublicOnly)
        : this(propertyExpression.GetPropertyInfo(), accessPolicy)
    {
    }

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="property">The property value.</param>
    /// <param name="accessPolicy">The access policy value.</param>
    public ReadWriteProperty(PropertyInfo? property, PropertyAccessPolicy accessPolicy = PropertyAccessPolicy.PublicOnly)
        : base(property, accessPolicy)
    {
        SetProperty = PropertyAccessorFactory.CreateSetter<T, TProperty>(Property, accessPolicy);
    }

    /// <summary>
    /// Performs the set operation.
    /// </summary>
    /// <param name="instance">The instance value.</param>
    /// <param name="value">The value.</param>
    public void Set(T instance, TProperty value) => SetProperty(instance, value);
}
