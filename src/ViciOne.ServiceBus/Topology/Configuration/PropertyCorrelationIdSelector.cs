using System;
using ViciOne.ServiceBus.Internals;
using ViciOne.ServiceBus.Topology;

namespace ViciOne.ServiceBus.Configuration;

/// <summary>
/// Provides a property correlation id selector implementation.
/// </summary>
/// <typeparam name="T">The t type.</typeparam>
public class PropertyCorrelationIdSelector<T> :
    ICorrelationIdSelector<T>
    where T : class
{
    readonly string _propertyName;

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="propertyName">The property name value.</param>
    public PropertyCorrelationIdSelector(string propertyName)
    {
        _propertyName = propertyName;
    }

    /// <summary>
    /// Attempts to get set correlation id.
    /// </summary>
    /// <param name="messageCorrelationId">The message correlation id value.</param>
    /// <returns><see langword="true" /> when the condition is satisfied; otherwise, <see langword="false" />.</returns>
    public bool TryGetSetCorrelationId([NotNullWhen(true)] out IMessageCorrelationId<T>? messageCorrelationId)
    {
        if (ReadPropertyCache<T>.TryGetProperty(_propertyName, out IReadProperty<T, Guid>? property))
        {
            messageCorrelationId = new PropertyMessageCorrelationId<T>(property);
            return true;
        }

        if (ReadPropertyCache<T>.TryGetProperty(_propertyName, out IReadProperty<T, Guid?>? nullableProperty))
        {
            messageCorrelationId = new NullablePropertyMessageCorrelationId<T>(nullableProperty);
            return true;
        }

        messageCorrelationId = null;
        return false;
    }
}
