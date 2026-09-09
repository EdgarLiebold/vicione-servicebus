using System;
using System.Linq.Expressions;
using System.Reflection;
using ViciOne.ServiceBus.Internals;

namespace ViciOne.ServiceBus.Metadata;

/// <summary>Reads and writes one instance property through cached accessors.</summary>
public class ReadWriteProperty : ReadOnlyProperty
{
    readonly Action<object, object?> _setter;

    /// <summary>Initializes cached accessors for a reflected property.</summary>
    /// <param name="property">The instance property to read and write.</param>
    /// <param name="accessPolicy">The accessibility boundary for both accessors.</param>
    public ReadWriteProperty(PropertyInfo property, PropertyAccessPolicy accessPolicy = PropertyAccessPolicy.PublicOnly)
        : base(property, accessPolicy)
    {
        _setter = PropertyAccessorFactory.CreateUntypedSetter(Property, accessPolicy);
    }

    /// <summary>Writes the property on an instance.</summary>
    /// <param name="instance">The target instance.</param>
    /// <param name="value">The new property value.</param>
    public void Set(object instance, object? value) => _setter(instance, value);
}


/// <summary>Reads and writes one instance property through target-typed cached accessors.</summary>
/// <typeparam name="T">The target type.</typeparam>
public class ReadWriteProperty<T> : ReadOnlyProperty<T>
{
    readonly Action<T, object?> _setter;

    /// <summary>Initializes target-typed accessors from a property expression.</summary>
    /// <param name="propertyExpression">An expression selecting the property on <typeparamref name="T" />.</param>
    /// <param name="accessPolicy">The accessibility boundary for both accessors.</param>
    public ReadWriteProperty(Expression<Func<T, object>> propertyExpression, PropertyAccessPolicy accessPolicy = PropertyAccessPolicy.PublicOnly)
        : this(propertyExpression.GetPropertyInfo(), accessPolicy)
    {
    }

    /// <summary>Initializes target-typed accessors from reflected property metadata.</summary>
    /// <param name="property">The instance property to read and write.</param>
    /// <param name="accessPolicy">The accessibility boundary for both accessors.</param>
    public ReadWriteProperty(PropertyInfo? property, PropertyAccessPolicy accessPolicy = PropertyAccessPolicy.PublicOnly)
        : base(property, accessPolicy)
    {
        _setter = PropertyAccessorFactory.CreateSetter<T>(Property, accessPolicy);
    }

    /// <summary>Writes the property on an instance.</summary>
    /// <param name="instance">The target instance.</param>
    /// <param name="value">The new property value.</param>
    public void Set(T instance, object? value) => _setter(instance, value);
}


/// <summary>Reads and writes one instance property through fully typed cached accessors.</summary>
/// <typeparam name="T">The target type.</typeparam>
/// <typeparam name="TProperty">The property type.</typeparam>
public class ReadWriteProperty<T, TProperty> : ReadOnlyProperty<T, TProperty>
{
    readonly Action<T, TProperty> _setter;

    /// <summary>Initializes fully typed accessors from a property expression.</summary>
    /// <param name="propertyExpression">An expression selecting the property on <typeparamref name="T" />.</param>
    /// <param name="accessPolicy">The accessibility boundary for both accessors.</param>
    public ReadWriteProperty(Expression<Func<T, object>> propertyExpression, PropertyAccessPolicy accessPolicy = PropertyAccessPolicy.PublicOnly)
        : this(propertyExpression.GetPropertyInfo(), accessPolicy)
    {
    }

    /// <summary>Initializes fully typed accessors from reflected property metadata.</summary>
    /// <param name="property">The <typeparamref name="TProperty" /> instance property to read and write.</param>
    /// <param name="accessPolicy">The accessibility boundary for both accessors.</param>
    public ReadWriteProperty(PropertyInfo? property, PropertyAccessPolicy accessPolicy = PropertyAccessPolicy.PublicOnly)
        : base(property, accessPolicy)
    {
        _setter = PropertyAccessorFactory.CreateSetter<T, TProperty>(Property, accessPolicy);
    }

    /// <summary>Writes the property on an instance.</summary>
    /// <param name="instance">The target instance.</param>
    /// <param name="value">The new property value.</param>
    public void Set(T instance, TProperty value) => _setter(instance, value);
}
