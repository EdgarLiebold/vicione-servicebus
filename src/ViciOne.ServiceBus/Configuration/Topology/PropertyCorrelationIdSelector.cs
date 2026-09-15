using System;
using ViciOne.ServiceBus.Internals.Reflection;
using ViciOne.ServiceBus.Topology;

namespace ViciOne.ServiceBus.Configuration;

/// <summary>Resolves a correlation identifier reader for a named message property.</summary>
/// <typeparam name="T">The message contract.</typeparam>
sealed class PropertyCorrelationIdSelector<T> :
    ICorrelationIdSelector<T>
    where T : class
{
    readonly string _propertyName;

    /// <summary>Creates a selector for the specified required or nullable <see cref="Guid" /> property.</summary>
    /// <param name="propertyName">The message property name.</param>
    public PropertyCorrelationIdSelector(string propertyName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(propertyName);
        _propertyName = propertyName;
    }

    /// <summary>Attempts to create a reader for the configured property.</summary>
    /// <param name="messageCorrelationId">The property-backed reader when the property is compatible.</param>
    /// <returns><see langword="true" /> when the property is a <see cref="Guid" /> or nullable <see cref="Guid" />; otherwise, <see langword="false" />.</returns>
    public bool TryGetCorrelationIdResolver([NotNullWhen(true)] out IMessageCorrelationId<T>? messageCorrelationId)
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
