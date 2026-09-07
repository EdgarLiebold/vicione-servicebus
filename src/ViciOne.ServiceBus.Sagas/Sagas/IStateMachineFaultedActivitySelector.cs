using System;

namespace ViciOne.ServiceBus.Sagas;

/// <summary>Defines the operations required by state machine faulted activity selector.</summary>
/// <typeparam name="TInstance">The instance type.</typeparam>
/// <typeparam name="TData">The data type.</typeparam>
/// <typeparam name="TException">The exception handled by the member.</typeparam>
public interface IStateMachineFaultedActivitySelector<TInstance, TData, TException>
    where TInstance : class, SagaStateMachineInstance
    where TData : class
    where TException : Exception
{
    /// <summary>An activity which accepts the instance and data from the event.</summary>
    /// <typeparam name="TActivity">The activity type.</typeparam>
    /// <returns>The exception activity binder produced by the operation.</returns>
    ExceptionActivityBinder<TInstance, TData, TException> OfType<TActivity>()
        where TActivity : class, IStateMachineActivity<TInstance, TData>;

    /// <summary>An activity that only accepts the instance, and does not require the event data.</summary>
    /// <typeparam name="TActivity">The activity type.</typeparam>
    /// <returns>The exception activity binder produced by the operation.</returns>
    ExceptionActivityBinder<TInstance, TData, TException> OfInstanceType<TActivity>()
        where TActivity : class, IStateMachineActivity<TInstance>;
}


/// <summary>Defines the operations required by state machine faulted activity selector.</summary>
/// <typeparam name="TInstance">The instance type.</typeparam>
/// <typeparam name="TException">The exception handled by the member.</typeparam>
public interface IStateMachineFaultedActivitySelector<TInstance, TException>
    where TInstance : class, SagaStateMachineInstance
    where TException : Exception
{
    /// <summary>An activity which accepts the instance and data from the event.</summary>
    /// <typeparam name="TActivity">The activity type.</typeparam>
    /// <returns>The exception activity binder produced by the operation.</returns>
    ExceptionActivityBinder<TInstance, TException> OfType<TActivity>()
        where TActivity : class, IStateMachineActivity<TInstance>;
}
