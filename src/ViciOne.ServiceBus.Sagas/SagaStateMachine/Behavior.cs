namespace ViciOne.ServiceBus.SagaStateMachine;

/// <summary>
/// A behavior is invoked by a state when an event is raised on the instance and embodies
/// the activities that are executed in response to the event.
/// </summary>
public static class Behavior
{
    /// <summary>Returns an empty pipe of the specified context type.</summary>
    /// <typeparam name="TSaga">The context type.</typeparam>
    /// <returns>The behavior produced by the operation.</returns>
    public static IBehavior<TSaga> Empty<TSaga>()
        where TSaga : class, ISagaStateMachineInstance
    {
        return Cached<TSaga>.EmptyBehavior;
    }

    /// <summary>Returns an empty configured value.</summary>
    /// <typeparam name="TSaga">The saga state managed by the member.</typeparam>
    /// <typeparam name="TMessage">The message contract processed by the member.</typeparam>
    /// <returns>The behavior produced by the operation.</returns>
    public static IBehavior<TSaga, TMessage> Empty<TSaga, TMessage>()
        where TSaga : class, ISagaStateMachineInstance
        where TMessage : class
    {
        return Cached<TSaga, TMessage>.EmptyBehavior;
    }

    /// <summary>Reports that the operation has faulted.</summary>
    /// <typeparam name="TSaga">The saga state managed by the member.</typeparam>
    /// <returns>The behavior produced by the operation.</returns>
    public static IBehavior<TSaga> Faulted<TSaga>()
        where TSaga : class, ISagaStateMachineInstance
    {
        return Cached<TSaga>.FaultedBehavior;
    }

    /// <summary>Reports that the operation has faulted.</summary>
    /// <typeparam name="TSaga">The saga state managed by the member.</typeparam>
    /// <typeparam name="TMessage">The message contract processed by the member.</typeparam>
    /// <returns>The behavior produced by the operation.</returns>
    public static IBehavior<TSaga, TMessage> Faulted<TSaga, TMessage>()
        where TSaga : class, ISagaStateMachineInstance
        where TMessage : class
    {
        return Cached<TSaga, TMessage>.FaultedBehavior;
    }


    static class Cached<TSaga>
        where TSaga : class, ISagaStateMachineInstance
    {
        internal static readonly IBehavior<TSaga> EmptyBehavior = new EmptyBehavior<TSaga>();
        internal static readonly IBehavior<TSaga> FaultedBehavior = new FaultedBehavior<TSaga>();
    }


    static class Cached<TSaga, TMessage>
        where TSaga : class, ISagaStateMachineInstance
        where TMessage : class
    {
        internal static readonly IBehavior<TSaga, TMessage> EmptyBehavior = new EmptyBehavior<TSaga, TMessage>();
        internal static readonly IBehavior<TSaga, TMessage> FaultedBehavior = new FaultedBehavior<TSaga, TMessage>();
    }
}
