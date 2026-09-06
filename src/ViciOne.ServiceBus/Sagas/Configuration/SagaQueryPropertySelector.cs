using System;

namespace ViciOne.ServiceBus.Configuration;

/// <summary>Selects saga query property values.</summary>
/// <typeparam name="TData">The data type.</typeparam>
/// <typeparam name="TProperty">The property type.</typeparam>
public class SagaQueryPropertySelector<TData, TProperty> :
    ISagaQueryPropertySelector<TData, TProperty>
    where TData : class
    where TProperty : class
{
    readonly Func<ConsumeContext<TData>, TProperty> _selector;

    /// <summary>Initializes a new instance.</summary>
    /// <param name="selector">The selector.</param>
    public SagaQueryPropertySelector(Func<ConsumeContext<TData>, TProperty> selector)
    {
        _selector = selector;
    }

    /// <summary>Attempts to get property.</summary>
    /// <param name="context">The context associated with the operation.</param>
    /// <param name="property">Receives the property produced by the operation.</param>
    /// <returns><see langword="true" /> when the condition is satisfied; otherwise, <see langword="false" />.</returns>
    public bool TryGetProperty(ConsumeContext<TData> context, out TProperty property)
    {
        property = _selector(context);

        return property != null;
    }
}
