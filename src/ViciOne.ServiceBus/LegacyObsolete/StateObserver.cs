namespace ViciOne.ServiceBus
{
}


namespace Automatonymous
{
    using System;
    using ViciOne.ServiceBus;


    [Obsolete("Deprecated, use IStateObserver instead")]
    public interface StateObserver<TSaga> :
        IStateObserver<TSaga>
        where TSaga : class, SagaStateMachineInstance
    {
    }
}
