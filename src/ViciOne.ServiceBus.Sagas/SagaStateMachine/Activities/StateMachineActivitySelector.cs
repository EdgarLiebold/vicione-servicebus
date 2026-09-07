namespace ViciOne.ServiceBus.SagaStateMachine;

/// <summary>Selects state machine activity values.</summary>
/// <typeparam name="TSaga">The saga state managed by the member.</typeparam>
public class StateMachineActivitySelector<TSaga> :
    IStateMachineActivitySelector<TSaga>
    where TSaga : class, SagaStateMachineInstance
{
    readonly EventActivityBinder<TSaga> _binder;

    /// <summary>Initializes a new instance.</summary>
    /// <param name="binder">The binder.</param>
    public StateMachineActivitySelector(EventActivityBinder<TSaga> binder)
    {
        _binder = binder;
    }

    /// <summary>Restricts the operation to the specified type.</summary>
    /// <typeparam name="TActivity">The activity type.</typeparam>
    /// <returns>The event activity binder produced by the operation.</returns>
    public EventActivityBinder<TSaga> OfType<TActivity>()
        where TActivity : class, IStateMachineActivity<TSaga>
    {
        var activity = new ContainerFactoryActivity<TSaga, TActivity>();

        return _binder.Add(activity);
    }
}


/// <summary>Selects state machine activity values.</summary>
/// <typeparam name="TSaga">The saga state managed by the member.</typeparam>
/// <typeparam name="TMessage">The message contract processed by the member.</typeparam>
public class StateMachineActivitySelector<TSaga, TMessage> :
    IStateMachineActivitySelector<TSaga, TMessage>
    where TSaga : class, SagaStateMachineInstance
    where TMessage : class
{
    readonly EventActivityBinder<TSaga, TMessage> _binder;

    /// <summary>Initializes a new instance.</summary>
    /// <param name="binder">The binder.</param>
    public StateMachineActivitySelector(EventActivityBinder<TSaga, TMessage> binder)
    {
        _binder = binder;
    }

    /// <summary>Restricts the operation to the specified type.</summary>
    /// <typeparam name="TActivity">The activity type.</typeparam>
    /// <returns>The event activity binder produced by the operation.</returns>
    public EventActivityBinder<TSaga, TMessage> OfType<TActivity>()
        where TActivity : class, IStateMachineActivity<TSaga, TMessage>
    {
        var activity = new ContainerFactoryActivity<TSaga, TMessage, TActivity>();

        return _binder.Add(activity);
    }

    /// <summary>Restricts the operation to the saga instance type.</summary>
    /// <typeparam name="TActivity">The activity type.</typeparam>
    /// <returns>The event activity binder produced by the operation.</returns>
    public EventActivityBinder<TSaga, TMessage> OfInstanceType<TActivity>()
        where TActivity : class, IStateMachineActivity<TSaga>
    {
        var activity = new ContainerFactoryActivity<TSaga, TActivity>();

        return _binder.Add(activity);
    }
}
