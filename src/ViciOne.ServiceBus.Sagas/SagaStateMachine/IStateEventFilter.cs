namespace ViciOne.ServiceBus.SagaStateMachine;

/// <summary>Processes state event pipeline stages.</summary>
/// <typeparam name="TSaga">The saga state managed by the member.</typeparam>
public interface IStateEventFilter<TSaga>
    where TSaga : class, ISagaStateMachineInstance
{
    /// <summary>Applies the configured filter.</summary>
    /// <param name="context">The context associated with the operation.</param>
    /// <returns><see langword="true" /> when the condition is satisfied; otherwise, <see langword="false" />.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="context" /> is null.</exception>
    bool Filter(IBehaviorContext<TSaga> context);

    /// <summary>Applies the configured filter.</summary>
    /// <typeparam name="T">The message contract type.</typeparam>
    /// <param name="context">The context associated with the operation.</param>
    /// <returns><see langword="true" /> when the condition is satisfied; otherwise, <see langword="false" />.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="context" /> is null.</exception>
    bool Filter<T>(IBehaviorContext<TSaga, T> context)
        where T : class;
}
