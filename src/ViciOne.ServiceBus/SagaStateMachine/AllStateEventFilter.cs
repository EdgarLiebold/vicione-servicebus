namespace ViciOne.ServiceBus.SagaStateMachine;

/// <summary>
/// Provides an all state event filter implementation.
/// </summary>
/// <typeparam name="TSaga">The t saga type.</typeparam>
public class AllStateEventFilter<TSaga> :
    IStateEventFilter<TSaga>
    where TSaga : class, SagaStateMachineInstance
{
    /// <summary>
    /// Performs the filter operation.
    /// </summary>
    /// <param name="context">The operation context.</param>
    /// <returns><see langword="true" /> when the condition is satisfied; otherwise, <see langword="false" />.</returns>
    public bool Filter(BehaviorContext<TSaga> context)
    {
        return true;
    }

    /// <summary>
    /// Performs the filter operation.
    /// </summary>
    /// <typeparam name="T">The t type.</typeparam>
    /// <param name="context">The operation context.</param>
    /// <returns><see langword="true" /> when the condition is satisfied; otherwise, <see langword="false" />.</returns>
    public bool Filter<T>(BehaviorContext<TSaga, T> context)
        where T : class
    {
        return true;
    }
}
