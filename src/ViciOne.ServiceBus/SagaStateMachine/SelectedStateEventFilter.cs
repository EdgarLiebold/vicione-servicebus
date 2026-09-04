namespace ViciOne.ServiceBus.SagaStateMachine;

/// <summary>
/// Provides a selected state event filter implementation.
/// </summary>
/// <typeparam name="TSaga">The t saga type.</typeparam>
/// <typeparam name="TMessage">The t message type.</typeparam>
public class SelectedStateEventFilter<TSaga, TMessage> :
    IStateEventFilter<TSaga>
    where TSaga : class, SagaStateMachineInstance
    where TMessage : class
{
    readonly StateMachineCondition<TSaga, TMessage> _filter;

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="filter">The filter value.</param>
    public SelectedStateEventFilter(StateMachineCondition<TSaga, TMessage> filter)
    {
        _filter = filter;
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
        if (context is BehaviorContext<TSaga, TMessage> filterContext)
            return _filter(filterContext);

        return false;
    }

    /// <summary>
    /// Performs the filter operation.
    /// </summary>
    /// <param name="context">The operation context.</param>
    /// <returns><see langword="true" /> when the condition is satisfied; otherwise, <see langword="false" />.</returns>
    public bool Filter(BehaviorContext<TSaga> context)
    {
        return false;
    }
}
