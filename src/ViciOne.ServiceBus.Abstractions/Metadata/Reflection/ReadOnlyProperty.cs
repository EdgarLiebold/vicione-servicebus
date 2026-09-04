using System;
using System.Linq.Expressions;
using System.Reflection;
using ViciOne.ServiceBus.Internals;

namespace ViciOne.ServiceBus.Metadata;

/// <summary>
/// Provides a read only property implementation.
/// </summary>
public class ReadOnlyProperty
{
    /// <summary>
    /// Defines the get property value.
    /// </summary>
    public readonly Func<object, object?> GetProperty;

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="property">The property value.</param>
    /// <param name="accessPolicy">The access policy value.</param>
    public ReadOnlyProperty(PropertyInfo property, PropertyAccessPolicy accessPolicy = PropertyAccessPolicy.PublicOnly)
    {
        Property = PropertyAccessorFactory.Validate(property);
        GetProperty = PropertyAccessorFactory.CreateUntypedGetter(Property, accessPolicy);
    }

    /// <summary>
    /// Gets the property value.
    /// </summary>
    public PropertyInfo Property { get; }

    /// <summary>
    /// Performs the get operation.
    /// </summary>
    /// <param name="instance">The instance value.</param>
    /// <returns>The result of the operation.</returns>
    public object? Get(object instance) => GetProperty(instance);
}


/// <summary>
/// Provides a read only property implementation.
/// </summary>
/// <typeparam name="T">The t type.</typeparam>
public class ReadOnlyProperty<T>
{
    /// <summary>
    /// Defines the get property value.
    /// </summary>
    public readonly Func<T, object?> GetProperty;

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="propertyExpression">The property expression value.</param>
    /// <param name="accessPolicy">The access policy value.</param>
    public ReadOnlyProperty(Expression<Func<T, object>> propertyExpression, PropertyAccessPolicy accessPolicy = PropertyAccessPolicy.PublicOnly)
        : this(propertyExpression.GetPropertyInfo(), accessPolicy)
    {
    }

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="property">The property value.</param>
    /// <param name="accessPolicy">The access policy value.</param>
    public ReadOnlyProperty(PropertyInfo? property, PropertyAccessPolicy accessPolicy = PropertyAccessPolicy.PublicOnly)
    {
        Property = PropertyAccessorFactory.Validate(property, typeof(T));
        GetProperty = PropertyAccessorFactory.CreateGetter<T>(Property, accessPolicy);
    }

    /// <summary>
    /// Gets the property value.
    /// </summary>
    public PropertyInfo Property { get; }

    /// <summary>
    /// Performs the get operation.
    /// </summary>
    /// <param name="instance">The instance value.</param>
    /// <returns>The result of the operation.</returns>
    public object? Get(T instance) => GetProperty(instance);
}


/// <summary>
/// Provides a read only property implementation.
/// </summary>
/// <typeparam name="T">The t type.</typeparam>
/// <typeparam name="TProperty">The t property type.</typeparam>
public class ReadOnlyProperty<T, TProperty>
{
    /// <summary>
    /// Defines the get property value.
    /// </summary>
    public readonly Func<T, TProperty> GetProperty;

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="propertyExpression">The property expression value.</param>
    /// <param name="accessPolicy">The access policy value.</param>
    public ReadOnlyProperty(Expression<Func<T, object>> propertyExpression, PropertyAccessPolicy accessPolicy = PropertyAccessPolicy.PublicOnly)
        : this(propertyExpression.GetPropertyInfo(), accessPolicy)
    {
    }

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="property">The property value.</param>
    /// <param name="accessPolicy">The access policy value.</param>
    public ReadOnlyProperty(PropertyInfo? property, PropertyAccessPolicy accessPolicy = PropertyAccessPolicy.PublicOnly)
    {
        Property = PropertyAccessorFactory.Validate(property, typeof(T), typeof(TProperty));
        GetProperty = PropertyAccessorFactory.CreateGetter<T, TProperty>(Property, accessPolicy);
    }

    /// <summary>
    /// Gets the property value.
    /// </summary>
    public PropertyInfo Property { get; }

    /// <summary>
    /// Performs the get operation.
    /// </summary>
    /// <param name="instance">The instance value.</param>
    /// <returns>The result of the operation.</returns>
    public TProperty Get(T instance) => GetProperty(instance);
}
