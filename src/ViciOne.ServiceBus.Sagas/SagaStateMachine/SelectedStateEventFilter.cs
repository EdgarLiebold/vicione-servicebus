namespace ViciOne.ServiceBus.SagaStateMachine;

/// <summary>Processes selected state event pipeline stages.</summary>
/// <typeparam name="TSaga">The saga state managed by the member.</typeparam>
/// <typeparam name="TMessage">The message contract processed by the member.</typeparam>
public class SelectedStateEventFilter<TSaga, TMessage> :
    IStateEventFilter<TSaga>
    where TSaga : class, ISagaStateMachineInstance
    where TMessage : class
{
    readonly StateMachineCondition<TSaga, TMessage> _filter;

    /// <summary>Initializes a new instance.</summary>
    /// <param name="filter">The filter to add to the pipeline.</param>
    public SelectedStateEventFilter(StateMachineCondition<TSaga, TMessage> filter)
    {
        _filter = filter;
    }

    /// <summary>Applies the configured filter.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="context">The context associated with the operation.</param>
    /// <returns><see langword="true" /> when the condition is satisfied; otherwise, <see langword="false" />.</returns>
    public bool Filter<T>(IBehaviorContext<TSaga, T> context)
        where T : class
    {
        if (context is IBehaviorContext<TSaga, TMessage> filterContext)
            return _filter(filterContext);

        return false;
    }

    /// <summary>Applies the configured filter.</summary>
    /// <param name="context">The context associated with the operation.</param>
    /// <returns><see langword="true" /> when the condition is satisfied; otherwise, <see langword="false" />.</returns>
    public bool Filter(IBehaviorContext<TSaga> context)
    {
        return false;
    }
}
