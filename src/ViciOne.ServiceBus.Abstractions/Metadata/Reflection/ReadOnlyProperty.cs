using System;
using System.Linq.Expressions;
using System.Reflection;
using ViciOne.ServiceBus.Internals;

namespace ViciOne.ServiceBus.Metadata;

/// <summary>Provides access to the read only property.</summary>
public class ReadOnlyProperty
{
    /// <summary>Exposes the get property used by the containing type.</summary>
    public readonly Func<object, object?> GetProperty;

    /// <summary>Initializes a new instance.</summary>
    /// <param name="property">The property.</param>
    /// <param name="accessPolicy">The access policy.</param>
    public ReadOnlyProperty(PropertyInfo property, PropertyAccessPolicy accessPolicy = PropertyAccessPolicy.PublicOnly)
    {
        Property = PropertyAccessorFactory.Validate(property);
        GetProperty = PropertyAccessorFactory.CreateUntypedGetter(Property, accessPolicy);
    }

    /// <summary>Gets the property.</summary>
    public PropertyInfo Property { get; }

    /// <summary>Retrieves the requested value.</summary>
    /// <param name="instance">The instance.</param>
    /// <returns>The requested value.</returns>
    public object? Get(object instance) => GetProperty(instance);
}


/// <summary>Provides access to the read only property.</summary>
/// <typeparam name="T">The value type.</typeparam>
public class ReadOnlyProperty<T>
{
    /// <summary>Exposes the get property used by the containing type.</summary>
    public readonly Func<T, object?> GetProperty;

    /// <summary>Initializes a new instance.</summary>
    /// <param name="propertyExpression">The property expression.</param>
    /// <param name="accessPolicy">The access policy.</param>
    public ReadOnlyProperty(Expression<Func<T, object>> propertyExpression, PropertyAccessPolicy accessPolicy = PropertyAccessPolicy.PublicOnly)
        : this(propertyExpression.GetPropertyInfo(), accessPolicy)
    {
    }

    /// <summary>Initializes a new instance.</summary>
    /// <param name="property">The property.</param>
    /// <param name="accessPolicy">The access policy.</param>
    public ReadOnlyProperty(PropertyInfo? property, PropertyAccessPolicy accessPolicy = PropertyAccessPolicy.PublicOnly)
    {
        Property = PropertyAccessorFactory.Validate(property, typeof(T));
        GetProperty = PropertyAccessorFactory.CreateGetter<T>(Property, accessPolicy);
    }

    /// <summary>Gets the property.</summary>
    public PropertyInfo Property { get; }

    /// <summary>Retrieves the requested value.</summary>
    /// <param name="instance">The instance.</param>
    /// <returns>The requested value.</returns>
    public object? Get(T instance) => GetProperty(instance);
}


/// <summary>Provides access to the read only property.</summary>
/// <typeparam name="T">The value type.</typeparam>
/// <typeparam name="TProperty">The property type.</typeparam>
public class ReadOnlyProperty<T, TProperty>
{
    /// <summary>Exposes the get property used by the containing type.</summary>
    public readonly Func<T, TProperty> GetProperty;

    /// <summary>Initializes a new instance.</summary>
    /// <param name="propertyExpression">The property expression.</param>
    /// <param name="accessPolicy">The access policy.</param>
    public ReadOnlyProperty(Expression<Func<T, object>> propertyExpression, PropertyAccessPolicy accessPolicy = PropertyAccessPolicy.PublicOnly)
        : this(propertyExpression.GetPropertyInfo(), accessPolicy)
    {
    }

    /// <summary>Initializes a new instance.</summary>
    /// <param name="property">The property.</param>
    /// <param name="accessPolicy">The access policy.</param>
    public ReadOnlyProperty(PropertyInfo? property, PropertyAccessPolicy accessPolicy = PropertyAccessPolicy.PublicOnly)
    {
        Property = PropertyAccessorFactory.Validate(property, typeof(T), typeof(TProperty));
        GetProperty = PropertyAccessorFactory.CreateGetter<T, TProperty>(Property, accessPolicy);
    }

    /// <summary>Gets the property.</summary>
    public PropertyInfo Property { get; }

    /// <summary>Retrieves the requested value.</summary>
    /// <param name="instance">The instance.</param>
    /// <returns>The requested value.</returns>
    public TProperty Get(T instance) => GetProperty(instance);
}
