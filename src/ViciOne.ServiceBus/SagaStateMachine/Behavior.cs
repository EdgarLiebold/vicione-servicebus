namespace ViciOne.ServiceBus.SagaStateMachine;

/// <summary>
/// A behavior is invoked by a state when an event is raised on the instance and embodies
/// the activities that are executed in response to the event.
/// </summary>
public static class Behavior
{
    /// <summary>
    /// Returns an empty pipe of the specified context type
    /// </summary>
    /// <typeparam name="TSaga">The context type</typeparam>
    /// <returns></returns>
    public static IBehavior<TSaga> Empty<TSaga>()
        where TSaga : class, SagaStateMachineInstance
    {
        return Cached<TSaga>.EmptyBehavior;
    }

    /// <summary>
    /// Performs the empty operation.
    /// </summary>
    /// <typeparam name="TSaga">The t saga type.</typeparam>
    /// <typeparam name="TMessage">The t message type.</typeparam>
    /// <returns>The result of the operation.</returns>
    public static IBehavior<TSaga, TMessage> Empty<TSaga, TMessage>()
        where TSaga : class, SagaStateMachineInstance
        where TMessage : class
    {
        return Cached<TSaga, TMessage>.EmptyBehavior;
    }

    /// <summary>
    /// Performs the faulted operation.
    /// </summary>
    /// <typeparam name="TSaga">The t saga type.</typeparam>
    /// <returns>The result of the operation.</returns>
    public static IBehavior<TSaga> Faulted<TSaga>()
        where TSaga : class, SagaStateMachineInstance
    {
        return Cached<TSaga>.FaultedBehavior;
    }

    /// <summary>
    /// Performs the faulted operation.
    /// </summary>
    /// <typeparam name="TSaga">The t saga type.</typeparam>
    /// <typeparam name="TMessage">The t message type.</typeparam>
    /// <returns>The result of the operation.</returns>
    public static IBehavior<TSaga, TMessage> Faulted<TSaga, TMessage>()
        where TSaga : class, SagaStateMachineInstance
        where TMessage : class
    {
        return Cached<TSaga, TMessage>.FaultedBehavior;
    }


    static class Cached<TSaga>
        where TSaga : class, SagaStateMachineInstance
    {
        internal static readonly IBehavior<TSaga> EmptyBehavior = new EmptyBehavior<TSaga>();
        internal static readonly IBehavior<TSaga> FaultedBehavior = new FaultedBehavior<TSaga>();
    }


    static class Cached<TSaga, TMessage>
        where TSaga : class, SagaStateMachineInstance
        where TMessage : class
    {
        internal static readonly IBehavior<TSaga, TMessage> EmptyBehavior = new EmptyBehavior<TSaga, TMessage>();
        internal static readonly IBehavior<TSaga, TMessage> FaultedBehavior = new FaultedBehavior<TSaga, TMessage>();
    }
}
