using System;
using ViciOne.ServiceBus.Internals.Reflection;

namespace ViciOne.ServiceBus.Topology;

internal class NullablePropertyMessageCorrelationId<T> :
    IMessageCorrelationId<T>
    where T : class
{
    readonly IReadProperty<T, Guid?> _property;

    public NullablePropertyMessageCorrelationId(IReadProperty<T, Guid?> property)
    {
        _property = property;
    }

    public bool TryGetCorrelationId(T message, out Guid correlationId)
    {
        Guid? id = _property.Get(message);
        if (id.HasValue && id.Value != Guid.Empty)
        {
            correlationId = id.Value;
            return true;
        }

        correlationId = Guid.Empty;
        return false;
    }
}
