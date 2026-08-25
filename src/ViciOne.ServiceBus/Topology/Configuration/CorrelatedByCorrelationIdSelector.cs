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
}
