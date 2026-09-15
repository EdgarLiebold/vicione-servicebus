using System;
using ViciOne.ServiceBus.Internals.Reflection;

namespace ViciOne.ServiceBus.Topology;

sealed class PropertyMessageCorrelationId<T> :
    IMessageCorrelationId<T>
    where T : class
{
    readonly IReadProperty<T, Guid> _property;

    public PropertyMessageCorrelationId(IReadProperty<T, Guid> property)
    {
        _property = property ?? throw new ArgumentNullException(nameof(property));
    }

    public bool TryGetCorrelationId(T message, out Guid correlationId)
    {
        ArgumentNullException.ThrowIfNull(message);

        correlationId = _property.Get(message);

        return correlationId != Guid.Empty;
    }
}
