// ViciOne modification: WP-F2-SERVICEBUS-IDENTITY, 2026-08-07.
namespace ViciOne.ServiceBus
{
}


namespace Automatonymous
{
    using System;
    using ViciOne.ServiceBus;


    [Obsolete]
    public interface Activity :
        IVisitable
    {
    }


    /// <summary>
    /// An activity is part of a behavior that is executed in order
    /// </summary>
    /// <typeparam name="TSaga"></typeparam>
    [Obsolete("Deprecated, use IStateMachineActivity<TSaga> instead", true)]
    public interface Activity<TSaga> :
        IStateMachineActivity<TSaga>,
        Activity
        where TSaga : class, SagaStateMachineInstance
    {
    }


    [Obsolete("Deprecated, use IStateMachineActivity<TSaga, TMessage> instead", true)]
    public interface Activity<TSaga, TMessage> :
        IStateMachineActivity<TSaga, TMessage>,
        Activity
        where TSaga : class, SagaStateMachineInstance
        where TMessage : class
    {
    }
}
