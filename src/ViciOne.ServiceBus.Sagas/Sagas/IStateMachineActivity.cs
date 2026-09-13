using System;
using System.Threading.Tasks;

namespace ViciOne.ServiceBus.Sagas;

/// <summary>Represents a state-machine activity that supports structural inspection.</summary>
public interface IStateMachineActivity :
    IVisitable
{
}


/// <summary>Represents an activity within a saga behavior pipeline.</summary>
/// <typeparam name="TSaga">The saga state managed by the member.</typeparam>
public interface IStateMachineActivity<TSaga> :
    IStateMachineActivity
    where TSaga : class, ISagaStateMachineInstance
{
    /// <summary>Executes the activity for an untyped event and continues with the next behavior.</summary>
    /// <param name="context">The behavior context.</param>
    /// <param name="next">The behavior that follows this activity.</param>
    /// <returns>An awaitable task.</returns>
    Task ExecuteAsync(IBehaviorContext<TSaga> context, IBehavior<TSaga> next);

    /// <summary>Executes the activity for a message event and continues with the next behavior.</summary>
    /// <typeparam name="T">The message contract carried by the event.</typeparam>
    /// <param name="context">The behavior context.</param>
    /// <param name="next">The behavior that follows this activity.</param>
    /// <returns>An awaitable task.</returns>
    Task ExecuteAsync<T>(IBehaviorContext<TSaga, T> context, IBehavior<TSaga, T> next)
        where T : class;

    /// <summary>Handles or forwards an exception raised while processing an untyped event.</summary>
    /// <typeparam name="TException">The exception handled by the member.</typeparam>
    /// <param name="context">The context associated with the operation.</param>
    /// <param name="next">The next pipeline stage to invoke.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    Task FaultedAsync<TException>(IBehaviorExceptionContext<TSaga, TException> context, IBehavior<TSaga> next)
        where TException : Exception;

    /// <summary>Handles or forwards an exception raised while processing a message event.</summary>
    /// <typeparam name="T">The message contract carried by the event.</typeparam>
    /// <typeparam name="TException">The exception handled by the member.</typeparam>
    /// <param name="context">The context associated with the operation.</param>
    /// <param name="next">The next pipeline stage to invoke.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    Task FaultedAsync<T, TException>(IBehaviorExceptionContext<TSaga, T, TException> context, IBehavior<TSaga, T> next)
        where TException : Exception
        where T : class;
}


/// <summary>Represents an activity within a message-specific saga behavior pipeline.</summary>
/// <typeparam name="TSaga">The saga state managed by the member.</typeparam>
/// <typeparam name="TMessage">The message contract processed by the member.</typeparam>
public interface IStateMachineActivity<TSaga, TMessage> :
    IStateMachineActivity
    where TSaga : class, ISagaStateMachineInstance
    where TMessage : class
{
    /// <summary>Executes the activity and continues with the next behavior.</summary>
    /// <param name="context">The behavior context.</param>
    /// <param name="next">The behavior that follows this activity.</param>
    /// <returns>An awaitable task.</returns>
    Task ExecuteAsync(IBehaviorContext<TSaga, TMessage> context, IBehavior<TSaga, TMessage> next);

    /// <summary>Handles or forwards an exception raised while executing the activity for the supplied message.</summary>
    /// <typeparam name="TException">The exception handled by the member.</typeparam>
    /// <param name="context">The context associated with the operation.</param>
    /// <param name="next">The next pipeline stage to invoke.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    Task FaultedAsync<TException>(IBehaviorExceptionContext<TSaga, TMessage, TException> context, IBehavior<TSaga, TMessage> next)
        where TException : Exception;
}
