// ViciOne modification: WP-F2-SERVICEBUS-IDENTITY, 2026-08-07.
namespace ViciOne.ServiceBus.Testing.Implementations
{
    using System;


    public class SagaInstance<T> :
        ISagaInstance<T>
        where T : class, ISaga
    {
        public SagaInstance(T saga)
        {
            Saga = saga;
        }

        public T Saga { get; }

        public Guid? ElementId => Saga.CorrelationId;
    }
}
