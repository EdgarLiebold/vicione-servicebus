using System;

namespace ViciOne.ServiceBus.Sagas;

/// <summary>
/// Defines the contract for state machine faulted activity selector.
/// </summary>
/// <typeparam name="TInstance">The t instance type.</typeparam>
/// <typeparam name="TData">The t data type.</typeparam>
/// <typeparam name="TException">The t exception type.</typeparam>
public interface IStateMachineFaultedActivitySelector<TInstance, TData, TException>
    where TInstance : class, SagaStateMachineInstance
    where TData : class
    where TException : Exception
{
    /// <summary>
    /// An activity which accepts the instance and data from the event
    /// </summary>
    /// <typeparam name="TActivity"></typeparam>
    /// <returns></returns>
    ExceptionActivityBinder<TInstance, TData, TException> OfType<TActivity>()
        where TActivity : class, IStateMachineActivity<TInstance, TData>;

    /// <summary>
    /// An activity that only accepts the instance, and does not require the event data
    /// </summary>
    /// <typeparam name="TActivity"></typeparam>
    /// <returns></returns>
    ExceptionActivityBinder<TInstance, TData, TException> OfInstanceType<TActivity>()
        where TActivity : class, IStateMachineActivity<TInstance>;
}


/// <summary>
/// Defines the contract for state machine faulted activity selector.
/// </summary>
/// <typeparam name="TInstance">The t instance type.</typeparam>
/// <typeparam name="TException">The t exception type.</typeparam>
public interface IStateMachineFaultedActivitySelector<TInstance, TException>
    where TInstance : class, SagaStateMachineInstance
    where TException : Exception
{
    /// <summary>
    /// An activity which accepts the instance and data from the event
    /// </summary>
    /// <typeparam name="TActivity"></typeparam>
    /// <returns></returns>
    ExceptionActivityBinder<TInstance, TException> OfType<TActivity>()
        where TActivity : class, IStateMachineActivity<TInstance>;
}
