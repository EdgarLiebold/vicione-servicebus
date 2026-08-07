// ViciOne modification: WP-F2-SERVICEBUS-IDENTITY, 2026-08-07.
namespace ViciOne.ServiceBus.Configuration
{
    using System;
    using Internals;
    using Topology;


    public class CorrelatedByCorrelationIdSelector<T> :
        ICorrelationIdSelector<T>
        where T : class
    {
        public bool TryGetSetCorrelationId(out IMessageCorrelationId<T> messageCorrelationId)
        {
            var correlatedByInterface = typeof(T).GetInterface<CorrelatedBy<Guid>>();
            if (correlatedByInterface != null)
            {
                var objectType = typeof(CorrelatedByMessageCorrelationId<>).MakeGenericType(typeof(T));
                messageCorrelationId = (IMessageCorrelationId<T>)Activator.CreateInstance(objectType);
                return true;
            }

            messageCorrelationId = null;
            return false;
        }
    }
}
