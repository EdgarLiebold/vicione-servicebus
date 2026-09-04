using System;

namespace ViciOne.ServiceBus.SagaStateMachine;

/// <summary>
/// Provides a state machine faulted activity selector implementation.
/// </summary>
/// <typeparam name="TSaga">The t saga type.</typeparam>
/// <typeparam name="TException">The t exception type.</typeparam>
public class StateMachineFaultedActivitySelector<TSaga, TException> :
    IStateMachineFaultedActivitySelector<TSaga, TException>
    where TSaga : class, SagaStateMachineInstance
    where TException : Exception
{
    readonly ExceptionActivityBinder<TSaga, TException> _binder;

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="binder">The binder value.</param>
    public StateMachineFaultedActivitySelector(ExceptionActivityBinder<TSaga, TException> binder)
    {
        _binder = binder;
    }

    /// <summary>
    /// Performs the of type operation.
    /// </summary>
    /// <typeparam name="TActivity">The t activity type.</typeparam>
    /// <returns>The result of the operation.</returns>
    public ExceptionActivityBinder<TSaga, TException> OfType<TActivity>()
        where TActivity : class, IStateMachineActivity<TSaga>
    {
        var activity = new FaultedContainerFactoryActivity<TSaga, TException, TActivity>();

        return _binder.Add(activity);
    }
}


/// <summary>
/// Provides a state machine faulted activity selector implementation.
/// </summary>
/// <typeparam name="TSaga">The t saga type.</typeparam>
/// <typeparam name="TMessage">The t message type.</typeparam>
/// <typeparam name="TException">The t exception type.</typeparam>
public class StateMachineFaultedActivitySelector<TSaga, TMessage, TException> :
    IStateMachineFaultedActivitySelector<TSaga, TMessage, TException>
    where TSaga : class, SagaStateMachineInstance
    where TMessage : class
    where TException : Exception
{
    readonly ExceptionActivityBinder<TSaga, TMessage, TException> _binder;

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="binder">The binder value.</param>
    public StateMachineFaultedActivitySelector(ExceptionActivityBinder<TSaga, TMessage, TException> binder)
    {
        _binder = binder;
    }

    /// <summary>
    /// Performs the of type operation.
    /// </summary>
    /// <typeparam name="TActivity">The t activity type.</typeparam>
    /// <returns>The result of the operation.</returns>
    public ExceptionActivityBinder<TSaga, TMessage, TException> OfType<TActivity>()
        where TActivity : class, IStateMachineActivity<TSaga, TMessage>
    {
        var activity = new FaultedContainerFactoryActivity<TSaga, TMessage, TException, TActivity>();

        return _binder.Add(activity);
    }

    /// <summary>
    /// Performs the of instance type operation.
    /// </summary>
    /// <typeparam name="TActivity">The t activity type.</typeparam>
    /// <returns>The result of the operation.</returns>
    public ExceptionActivityBinder<TSaga, TMessage, TException> OfInstanceType<TActivity>()
        where TActivity : class, IStateMachineActivity<TSaga>
    {
        var activity = new FaultedContainerFactoryActivity<TSaga, TException, TActivity>();

        return _binder.Add(activity);
    }
}
