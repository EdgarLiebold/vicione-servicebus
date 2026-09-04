namespace ViciOne.ServiceBus.Sagas;

/// <summary>
/// Defines the contract for state machine activity selector.
/// </summary>
/// <typeparam name="TInstance">The t instance type.</typeparam>
/// <typeparam name="TData">The t data type.</typeparam>
public interface IStateMachineActivitySelector<TInstance, TData>
    where TInstance : class, SagaStateMachineInstance
    where TData : class
{
    /// <summary>
    /// An activity which accepts the instance and data from the event
    /// </summary>
    /// <typeparam name="TActivity"></typeparam>
    /// <returns></returns>
    EventActivityBinder<TInstance, TData> OfType<TActivity>()
        where TActivity : class, IStateMachineActivity<TInstance, TData>;

    /// <summary>
    /// An activity that only accepts the instance, and does not require the event data
    /// </summary>
    /// <typeparam name="TActivity"></typeparam>
    /// <returns></returns>
    EventActivityBinder<TInstance, TData> OfInstanceType<TActivity>()
        where TActivity : class, IStateMachineActivity<TInstance>;
}


/// <summary>
/// Defines the contract for state machine activity selector.
/// </summary>
/// <typeparam name="TInstance">The t instance type.</typeparam>
public interface IStateMachineActivitySelector<TInstance>
    where TInstance : class, SagaStateMachineInstance
{
    /// <summary>
    /// An activity which accepts the instance and data from the event
    /// </summary>
    /// <typeparam name="TActivity"></typeparam>
    /// <returns></returns>
    EventActivityBinder<TInstance> OfType<TActivity>()
        where TActivity : class, IStateMachineActivity<TInstance>;
}
