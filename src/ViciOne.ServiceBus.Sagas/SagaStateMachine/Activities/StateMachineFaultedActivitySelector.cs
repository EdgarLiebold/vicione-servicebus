using System;

namespace ViciOne.ServiceBus.SagaStateMachine;

/// <summary>Selects state machine faulted activity values.</summary>
/// <typeparam name="TSaga">The saga state managed by the member.</typeparam>
/// <typeparam name="TException">The exception handled by the member.</typeparam>
public class StateMachineFaultedActivitySelector<TSaga, TException> :
    IStateMachineFaultedActivitySelector<TSaga, TException>
    where TSaga : class, ISagaStateMachineInstance
    where TException : Exception
{
    readonly IExceptionActivityBinder<TSaga, TException> _binder;

    /// <summary>Initializes a new instance.</summary>
    /// <param name="binder">The binder.</param>
    /// <exception cref="ArgumentNullException"><paramref name="binder" /> is <see langword="null" />.</exception>
    public StateMachineFaultedActivitySelector(IExceptionActivityBinder<TSaga, TException> binder)
    {
        _binder = binder ?? throw new ArgumentNullException(nameof(binder));
    }

    /// <summary>Restricts the operation to the specified type.</summary>
    /// <typeparam name="TActivity">The activity type.</typeparam>
    /// <returns>The exception activity binder produced by the operation.</returns>
    public IExceptionActivityBinder<TSaga, TException> OfType<TActivity>()
        where TActivity : class, IStateMachineActivity<TSaga>
    {
        var activity = new FaultedContainerFactoryActivity<TSaga, TException, TActivity>();

        return _binder.Add(activity);
    }
}


/// <summary>Selects state machine faulted activity values.</summary>
/// <typeparam name="TSaga">The saga state managed by the member.</typeparam>
/// <typeparam name="TMessage">The message contract processed by the member.</typeparam>
/// <typeparam name="TException">The exception handled by the member.</typeparam>
public class StateMachineFaultedActivitySelector<TSaga, TMessage, TException> :
    IStateMachineFaultedActivitySelector<TSaga, TMessage, TException>
    where TSaga : class, ISagaStateMachineInstance
    where TMessage : class
    where TException : Exception
{
    readonly IExceptionActivityBinder<TSaga, TMessage, TException> _binder;

    /// <summary>Initializes a new instance.</summary>
    /// <param name="binder">The binder.</param>
    /// <exception cref="ArgumentNullException"><paramref name="binder" /> is <see langword="null" />.</exception>
    public StateMachineFaultedActivitySelector(IExceptionActivityBinder<TSaga, TMessage, TException> binder)
    {
        _binder = binder ?? throw new ArgumentNullException(nameof(binder));
    }

    /// <summary>Restricts the operation to the specified type.</summary>
    /// <typeparam name="TActivity">The activity type.</typeparam>
    /// <returns>The exception activity binder produced by the operation.</returns>
    public IExceptionActivityBinder<TSaga, TMessage, TException> OfType<TActivity>()
        where TActivity : class, IStateMachineActivity<TSaga, TMessage>
    {
        var activity = new FaultedContainerFactoryActivity<TSaga, TMessage, TException, TActivity>();

        return _binder.Add(activity);
    }

    /// <summary>Restricts the operation to the saga instance type.</summary>
    /// <typeparam name="TActivity">The activity type.</typeparam>
    /// <returns>The exception activity binder produced by the operation.</returns>
    public IExceptionActivityBinder<TSaga, TMessage, TException> OfInstanceType<TActivity>()
        where TActivity : class, IStateMachineActivity<TSaga>
    {
        var activity = new FaultedContainerFactoryActivity<TSaga, TException, TActivity>();

        return _binder.Add(activity);
    }
}
