using System;
using System.Collections.Generic;

namespace ViciOne.ServiceBus.Configuration;

/// <summary>
/// Provides a not default value type saga query property selector implementation.
/// </summary>
/// <typeparam name="TData">The t data type.</typeparam>
/// <typeparam name="TProperty">The t property type.</typeparam>
public class NotDefaultValueTypeSagaQueryPropertySelector<TData, TProperty> :
    ISagaQueryPropertySelector<TData, TProperty>
    where TData : class
    where TProperty : struct
{
    readonly Func<ConsumeContext<TData>, TProperty> _selector;

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="selector">The selector value.</param>
    public NotDefaultValueTypeSagaQueryPropertySelector(Func<ConsumeContext<TData>, TProperty> selector)
    {
        _selector = selector;
    }

    /// <summary>
    /// Attempts to get property.
    /// </summary>
    /// <param name="context">The operation context.</param>
    /// <param name="property">The property value.</param>
    /// <returns><see langword="true" /> when the condition is satisfied; otherwise, <see langword="false" />.</returns>
    public bool TryGetProperty(ConsumeContext<TData> context, out TProperty property)
    {
        property = _selector(context);

        return !EqualityComparer<TProperty>.Default.Equals(property, default);
    }
}
