using System;
using ViciOne.ServiceBus.Internals;
using ViciOne.ServiceBus.Topology;

namespace ViciOne.ServiceBus.Configuration;

public class CorrelatedByCorrelationIdSelector<T> :
    ICorrelationIdSelector<T>
    where T : class
{
    public bool TryGetSetCorrelationId(out IMessageCorrelationId<T> messageCorrelationId)
    {
        if (typeof(T).ImplementsInterface<CorrelatedBy<Guid>>())
        {
            var objectType = typeof(CorrelatedByMessageCorrelationId<>).MakeGenericType(typeof(T));
            messageCorrelationId = (IMessageCorrelationId<T>)Activator.CreateInstance(objectType);
            return true;
        }

        messageCorrelationId = null;
        return false;
    }
}
