// ViciOne modification: WP-F2-SERVICEBUS-IDENTITY, 2026-08-07.
namespace ViciOne.ServiceBus
{
}


namespace Automatonymous
{
    using System;
    using ViciOne.ServiceBus;


    [Obsolete("Deprecated, use IEventObserver instead")]
    public interface EventObserver<TSaga> :
        IEventObserver<TSaga>
        where TSaga : class, SagaStateMachineInstance
    {
    }
}
