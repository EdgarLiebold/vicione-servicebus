using System;
using ViciOne.ServiceBus.Internals.Reflection;

namespace ViciOne.ServiceBus.Topology;

internal class PropertyMessageCorrelationId<T> :
    IMessageCorrelationId<T>
    where T : class
{
    readonly IReadProperty<T, Guid> _property;

    public PropertyMessageCorrelationId(IReadProperty<T, Guid> property)
    {
        _property = property;
    }

    public bool TryGetCorrelationId(T message, out Guid correlationId)
    {
        correlationId = _property.Get(message);

        return correlationId != Guid.Empty;
    }
}
