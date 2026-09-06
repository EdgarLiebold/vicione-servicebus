using System;
using System.Linq.Expressions;
using System.Reflection;
using ViciOne.ServiceBus.Initializers;
using ViciOne.ServiceBus.Transformation;

namespace ViciOne.ServiceBus.Configuration;

/// <summary>Configures transform.</summary>
/// <typeparam name="TInput">The input type.</typeparam>
public interface ITransformConfigurator<TInput>
    where TInput : class
{
    /// <summary>Specifies if the message should be replaced, meaning modified in-place, instead of creating a new message.</summary>
    bool Replace { set; }

    /// <summary>Set the specified message property to the default value (ignoring the input value).</summary>
    /// <typeparam name="TProperty">The property type.</typeparam>
    /// <param name="propertyExpression">The property expression.</param>
    void Default<TProperty>(Expression<Func<TInput, TProperty>> propertyExpression);

    /// <summary>Set the specified property to a constant value.</summary>
    /// <typeparam name="TProperty">The property type.</typeparam>
    /// <param name="propertyExpression">The property expression.</param>
    /// <param name="value">The value to process.</param>
    void Set<TProperty>(Expression<Func<TInput, TProperty>> propertyExpression, TProperty? value);

    /// <summary>Set the property to the value, using the source context to create/select the value.</summary>
    /// <typeparam name="TProperty">The property type.</typeparam>
    /// <param name="propertyExpression">The property select expression.</param>
    /// <param name="valueProvider">The method to return the property.</param>
    void Set<TProperty>(Expression<Func<TInput, TProperty>> propertyExpression, Func<TransformPropertyContext<TProperty, TInput>, TProperty> valueProvider);

    /// <summary>Set the property to the value, using the property provider specified.</summary>
    /// <typeparam name="TProperty">The property type.</typeparam>
    /// <param name="property">The property.</param>
    /// <param name="propertyProvider">The property provider.</param>
    void Set<TProperty>(PropertyInfo property, IPropertyProvider<TInput, TProperty> propertyProvider);

    /// <summary>Transform the property, but leave it unchanged on the input.</summary>
    /// <typeparam name="TProperty">The property type.</typeparam>
    /// <param name="property">The property.</param>
    /// <param name="propertyProvider">The property provider.</param>
    void Transform<TProperty>(PropertyInfo property, IPropertyProvider<TInput, TProperty> propertyProvider);
}
