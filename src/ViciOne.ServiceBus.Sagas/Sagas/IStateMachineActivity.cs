using System;
using System.Threading.Tasks;

namespace ViciOne.ServiceBus.Sagas;

/// <summary>Defines the operations required by state machine activity.</summary>
public interface IStateMachineActivity :
    IVisitable
{
}


/// <summary>An activity is part of a behavior that is executed in order.</summary>
/// <typeparam name="TSaga">The saga state managed by the member.</typeparam>
public interface IStateMachineActivity<TSaga> :
    IStateMachineActivity
    where TSaga : class, SagaStateMachineInstance
{
    /// <summary>Execute the activity with the given behavior context.</summary>
    /// <param name="context">The behavior context.</param>
    /// <param name="next">The behavior that follows this activity.</param>
    /// <returns>An awaitable task.</returns>
    Task ExecuteAsync(BehaviorContext<TSaga> context, IBehavior<TSaga> next);

    /// <summary>Execute the activity with the given behavior context.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="context">The behavior context.</param>
    /// <param name="next">The behavior that follows this activity.</param>
    /// <returns>An awaitable task.</returns>
    Task ExecuteAsync<T>(BehaviorContext<TSaga, T> context, IBehavior<TSaga, T> next)
        where T : class;

    /// <summary>The exception path through the behavior allows activities to catch and handle exceptions.</summary>
    /// <typeparam name="TException">The exception handled by the member.</typeparam>
    /// <param name="context">The context associated with the operation.</param>
    /// <param name="next">The next pipeline stage to invoke.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    Task FaultedAsync<TException>(BehaviorExceptionContext<TSaga, TException> context, IBehavior<TSaga> next)
        where TException : Exception;

    /// <summary>The exception path through the behavior allows activities to catch and handle exceptions.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <typeparam name="TException">The exception handled by the member.</typeparam>
    /// <param name="context">The context associated with the operation.</param>
    /// <param name="next">The next pipeline stage to invoke.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    Task FaultedAsync<T, TException>(BehaviorExceptionContext<TSaga, T, TException> context, IBehavior<TSaga, T> next)
        where TException : Exception
        where T : class;
}


/// <summary>An activity is part of a behavior that is executed in order.</summary>
/// <typeparam name="TSaga">The saga state managed by the member.</typeparam>
/// <typeparam name="TMessage">The message contract processed by the member.</typeparam>
public interface IStateMachineActivity<TSaga, TMessage> :
    IStateMachineActivity
    where TSaga : class, SagaStateMachineInstance
    where TMessage : class
{
    /// <summary>Execute the activity with the given behavior context.</summary>
    /// <param name="context">The behavior context.</param>
    /// <param name="next">The behavior that follows this activity.</param>
    /// <returns>An awaitable task.</returns>
    Task ExecuteAsync(BehaviorContext<TSaga, TMessage> context, IBehavior<TSaga, TMessage> next);

    /// <summary>The exception path through the behavior allows activities to catch and handle exceptions.</summary>
    /// <typeparam name="TException">The exception handled by the member.</typeparam>
    /// <param name="context">The context associated with the operation.</param>
    /// <param name="next">The next pipeline stage to invoke.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    Task FaultedAsync<TException>(BehaviorExceptionContext<TSaga, TMessage, TException> context, IBehavior<TSaga, TMessage> next)
        where TException : Exception;
}
