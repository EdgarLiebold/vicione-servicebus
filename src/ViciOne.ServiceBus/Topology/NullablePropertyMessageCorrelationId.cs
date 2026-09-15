using System;
using ViciOne.ServiceBus.Internals.Reflection;

namespace ViciOne.ServiceBus.Topology;

sealed class NullablePropertyMessageCorrelationId<T> :
    IMessageCorrelationId<T>
    where T : class
{
    readonly IReadProperty<T, Guid?> _property;

    public NullablePropertyMessageCorrelationId(IReadProperty<T, Guid?> property)
    {
        _property = property ?? throw new ArgumentNullException(nameof(property));
    }

    public bool TryGetCorrelationId(T message, out Guid correlationId)
    {
        ArgumentNullException.ThrowIfNull(message);

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
