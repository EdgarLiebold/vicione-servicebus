namespace ViciOne.ServiceBus.SagaStateMachine;

/// <summary>Processes all state event pipeline stages.</summary>
/// <typeparam name="TSaga">The saga state managed by the member.</typeparam>
public class AllStateEventFilter<TSaga> :
    IStateEventFilter<TSaga>
    where TSaga : class, ISagaStateMachineInstance
{
    /// <summary>Applies the configured filter.</summary>
    /// <param name="context">The context associated with the operation.</param>
    /// <returns><see langword="true" /> when the condition is satisfied; otherwise, <see langword="false" />.</returns>
    public bool Filter(IBehaviorContext<TSaga> context)
    {
        return true;
    }

    /// <summary>Applies the configured filter.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="context">The context associated with the operation.</param>
    /// <returns><see langword="true" /> when the condition is satisfied; otherwise, <see langword="false" />.</returns>
    public bool Filter<T>(IBehaviorContext<TSaga, T> context)
        where T : class
    {
        return true;
    }
}
