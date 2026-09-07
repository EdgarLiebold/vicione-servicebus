namespace ViciOne.ServiceBus.SagaStateMachine;

/// <summary>Processes state event pipeline stages.</summary>
/// <typeparam name="TSaga">The saga state managed by the member.</typeparam>
public interface IStateEventFilter<TSaga>
    where TSaga : class, SagaStateMachineInstance
{
    /// <summary>Applies the configured filter.</summary>
    /// <param name="context">The context associated with the operation.</param>
    /// <returns><see langword="true" /> when the condition is satisfied; otherwise, <see langword="false" />.</returns>
    bool Filter(BehaviorContext<TSaga> context);

    /// <summary>Applies the configured filter.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="context">The context associated with the operation.</param>
    /// <returns><see langword="true" /> when the condition is satisfied; otherwise, <see langword="false" />.</returns>
    bool Filter<T>(BehaviorContext<TSaga, T> context)
        where T : class;
}
