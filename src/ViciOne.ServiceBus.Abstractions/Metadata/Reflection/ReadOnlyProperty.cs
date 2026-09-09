using System;
using System.Linq.Expressions;
using System.Reflection;
using ViciOne.ServiceBus.Internals;

namespace ViciOne.ServiceBus.Metadata;

/// <summary>Reads one instance property through a cached accessor.</summary>
public class ReadOnlyProperty
{
    readonly Func<object, object?> _getter;

    /// <summary>Initializes a cached property reader.</summary>
    /// <param name="property">The instance property to read.</param>
    /// <param name="accessPolicy">The accessibility boundary for the getter.</param>
    public ReadOnlyProperty(PropertyInfo property, PropertyAccessPolicy accessPolicy = PropertyAccessPolicy.PublicOnly)
    {
        Property = PropertyAccessorFactory.Validate(property);
        _getter = PropertyAccessorFactory.CreateUntypedGetter(Property, accessPolicy);
    }

    /// <summary>Gets the reflected property represented by this accessor.</summary>
    public PropertyInfo Property { get; }

    /// <summary>Reads the property from an instance.</summary>
    /// <param name="instance">The target instance.</param>
    /// <returns>The property value.</returns>
    public object? Get(object instance) => _getter(instance);
}


/// <summary>Reads one instance property through a target-typed cached accessor.</summary>
/// <typeparam name="T">The target type.</typeparam>
public class ReadOnlyProperty<T>
{
    readonly Func<T, object?> _getter;

    /// <summary>Initializes a cached reader from a property expression.</summary>
    /// <param name="propertyExpression">An expression selecting the property on <typeparamref name="T" />.</param>
    /// <param name="accessPolicy">The accessibility boundary for the getter.</param>
    public ReadOnlyProperty(Expression<Func<T, object>> propertyExpression, PropertyAccessPolicy accessPolicy = PropertyAccessPolicy.PublicOnly)
        : this(propertyExpression.GetPropertyInfo(), accessPolicy)
    {
    }

    /// <summary>Initializes a cached reader from reflected property metadata.</summary>
    /// <param name="property">The instance property to read.</param>
    /// <param name="accessPolicy">The accessibility boundary for the getter.</param>
    public ReadOnlyProperty(PropertyInfo? property, PropertyAccessPolicy accessPolicy = PropertyAccessPolicy.PublicOnly)
    {
        Property = PropertyAccessorFactory.Validate(property, typeof(T));
        _getter = PropertyAccessorFactory.CreateGetter<T>(Property, accessPolicy);
    }

    /// <summary>Gets the reflected property represented by this accessor.</summary>
    public PropertyInfo Property { get; }

    /// <summary>Reads the property from an instance.</summary>
    /// <param name="instance">The target instance.</param>
    /// <returns>The boxed property value.</returns>
    public object? Get(T instance) => _getter(instance);
}


/// <summary>Reads one instance property through a fully typed cached accessor.</summary>
/// <typeparam name="T">The target type.</typeparam>
/// <typeparam name="TProperty">The property type.</typeparam>
public class ReadOnlyProperty<T, TProperty>
{
    readonly Func<T, TProperty> _getter;

    /// <summary>Initializes a fully typed reader from a property expression.</summary>
    /// <param name="propertyExpression">An expression selecting the property on <typeparamref name="T" />.</param>
    /// <param name="accessPolicy">The accessibility boundary for the getter.</param>
    public ReadOnlyProperty(Expression<Func<T, object>> propertyExpression, PropertyAccessPolicy accessPolicy = PropertyAccessPolicy.PublicOnly)
        : this(propertyExpression.GetPropertyInfo(), accessPolicy)
    {
    }

    /// <summary>Initializes a fully typed reader from reflected property metadata.</summary>
    /// <param name="property">The <typeparamref name="TProperty" /> instance property to read.</param>
    /// <param name="accessPolicy">The accessibility boundary for the getter.</param>
    public ReadOnlyProperty(PropertyInfo? property, PropertyAccessPolicy accessPolicy = PropertyAccessPolicy.PublicOnly)
    {
        Property = PropertyAccessorFactory.Validate(property, typeof(T), typeof(TProperty));
        _getter = PropertyAccessorFactory.CreateGetter<T, TProperty>(Property, accessPolicy);
    }

    /// <summary>Gets the reflected property represented by this accessor.</summary>
    public PropertyInfo Property { get; }

    /// <summary>Reads the property from an instance.</summary>
    /// <param name="instance">The target instance.</param>
    /// <returns>The property value.</returns>
    public TProperty Get(T instance) => _getter(instance);
}
