namespace ViciOne.ServiceBus.SagaStateMachine;

/// <summary>
/// Provides a state machine activity selector implementation.
/// </summary>
/// <typeparam name="TSaga">The t saga type.</typeparam>
public class StateMachineActivitySelector<TSaga> :
    IStateMachineActivitySelector<TSaga>
    where TSaga : class, SagaStateMachineInstance
{
    readonly EventActivityBinder<TSaga> _binder;

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="binder">The binder value.</param>
    public StateMachineActivitySelector(EventActivityBinder<TSaga> binder)
    {
        _binder = binder;
    }

    /// <summary>
    /// Performs the of type operation.
    /// </summary>
    /// <typeparam name="TActivity">The t activity type.</typeparam>
    /// <returns>The result of the operation.</returns>
    public EventActivityBinder<TSaga> OfType<TActivity>()
        where TActivity : class, IStateMachineActivity<TSaga>
    {
        var activity = new ContainerFactoryActivity<TSaga, TActivity>();

        return _binder.Add(activity);
    }
}


/// <summary>
/// Provides a state machine activity selector implementation.
/// </summary>
/// <typeparam name="TSaga">The t saga type.</typeparam>
/// <typeparam name="TMessage">The t message type.</typeparam>
public class StateMachineActivitySelector<TSaga, TMessage> :
    IStateMachineActivitySelector<TSaga, TMessage>
    where TSaga : class, SagaStateMachineInstance
    where TMessage : class
{
    readonly EventActivityBinder<TSaga, TMessage> _binder;

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="binder">The binder value.</param>
    public StateMachineActivitySelector(EventActivityBinder<TSaga, TMessage> binder)
    {
        _binder = binder;
    }

    /// <summary>
    /// Performs the of type operation.
    /// </summary>
    /// <typeparam name="TActivity">The t activity type.</typeparam>
    /// <returns>The result of the operation.</returns>
    public EventActivityBinder<TSaga, TMessage> OfType<TActivity>()
        where TActivity : class, IStateMachineActivity<TSaga, TMessage>
    {
        var activity = new ContainerFactoryActivity<TSaga, TMessage, TActivity>();

        return _binder.Add(activity);
    }

    /// <summary>
    /// Performs the of instance type operation.
    /// </summary>
    /// <typeparam name="TActivity">The t activity type.</typeparam>
    /// <returns>The result of the operation.</returns>
    public EventActivityBinder<TSaga, TMessage> OfInstanceType<TActivity>()
        where TActivity : class, IStateMachineActivity<TSaga>
    {
        var activity = new ContainerFactoryActivity<TSaga, TActivity>();

        return _binder.Add(activity);
    }
}
