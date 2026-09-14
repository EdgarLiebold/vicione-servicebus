using System;
using ViciOne.ServiceBus.Internals.Reflection;
using ViciOne.ServiceBus.Topology;

namespace ViciOne.ServiceBus.Configuration;

/// <summary>Selects property correlation id values.</summary>
/// <typeparam name="T">The value type.</typeparam>
public class PropertyCorrelationIdSelector<T> :
    ICorrelationIdSelector<T>
    where T : class
{
    readonly string _propertyName;

    /// <summary>Initializes a new instance.</summary>
    /// <param name="propertyName">The property name.</param>
    public PropertyCorrelationIdSelector(string propertyName)
    {
        _propertyName = propertyName;
    }

    /// <summary>Attempts to get set correlation id.</summary>
    /// <param name="messageCorrelationId">Receives the message correlation id produced by the operation.</param>
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
