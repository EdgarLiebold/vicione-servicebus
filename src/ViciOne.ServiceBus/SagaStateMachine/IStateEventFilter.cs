namespace ViciOne.ServiceBus.SagaStateMachine;

/// <summary>
/// Defines the contract for state event filter.
/// </summary>
/// <typeparam name="TSaga">The t saga type.</typeparam>
public interface IStateEventFilter<TSaga>
    where TSaga : class, SagaStateMachineInstance
{
    /// <summary>
    /// Performs the filter operation.
    /// </summary>
    /// <param name="context">The operation context.</param>
    /// <returns><see langword="true" /> when the condition is satisfied; otherwise, <see langword="false" />.</returns>
    bool Filter(BehaviorContext<TSaga> context);

    /// <summary>
    /// Performs the filter operation.
    /// </summary>
    /// <typeparam name="T">The t type.</typeparam>
    /// <param name="context">The operation context.</param>
    /// <returns><see langword="true" /> when the condition is satisfied; otherwise, <see langword="false" />.</returns>
    bool Filter<T>(BehaviorContext<TSaga, T> context)
        where T : class;
}
