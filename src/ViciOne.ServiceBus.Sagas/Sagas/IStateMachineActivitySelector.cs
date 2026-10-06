namespace ViciOne.ServiceBus.Sagas;

/// <summary>Defines the operations required by state machine activity selector.</summary>
/// <typeparam name="TInstance">The instance type.</typeparam>
/// <typeparam name="TData">The data type.</typeparam>
public interface IStateMachineActivitySelector<TInstance, TData>
    where TInstance : class, ISagaStateMachineInstance
    where TData : class
{
    /// <summary>An activity which accepts the instance and data from the event.</summary>
    /// <typeparam name="TActivity">The activity type.</typeparam>
    /// <returns>The event activity binder produced by the operation.</returns>
    IEventActivityBinder<TInstance, TData> OfType<TActivity>()
        where TActivity : class, IStateMachineActivity<TInstance, TData>;

    /// <summary>An activity that only accepts the instance, and does not require the event data.</summary>
    /// <typeparam name="TActivity">The activity type.</typeparam>
    /// <returns>The event activity binder produced by the operation.</returns>
    IEventActivityBinder<TInstance, TData> OfInstanceType<TActivity>()
        where TActivity : class, IStateMachineActivity<TInstance>;
}


/// <summary>Defines the operations required by state machine activity selector.</summary>
/// <typeparam name="TInstance">The instance type.</typeparam>
public interface IStateMachineActivitySelector<TInstance>
    where TInstance : class, ISagaStateMachineInstance
{
    /// <summary>An activity that accepts the saga instance without requiring an event data contract.</summary>
    /// <typeparam name="TActivity">The activity type.</typeparam>
    /// <returns>The event activity binder produced by the operation.</returns>
    IEventActivityBinder<TInstance> OfType<TActivity>()
        where TActivity : class, IStateMachineActivity<TInstance>;
}
