using System;
using System.Threading.Tasks;

namespace ViciOne.ServiceBus.Sagas;

/// <summary>
/// A behavior is a chain of activities invoked by a state
/// </summary>
/// <typeparam name="TInstance">The state type</typeparam>
public interface IBehavior<TInstance> :
    IVisitable
    where TInstance : class, SagaStateMachineInstance
{
    /// <summary>
    /// Execute the activity with the given behavior context
    /// </summary>
    /// <param name="context">The behavior context</param>
    /// <returns>An awaitable task</returns>
    Task ExecuteAsync(BehaviorContext<TInstance> context);

    /// <summary>
    /// Execute the activity with the given behavior context
    /// </summary>
    /// <param name="context">The behavior context</param>
    /// <returns>An awaitable task</returns>
    Task ExecuteAsync<T>(BehaviorContext<TInstance, T> context)
        where T : class;

    /// <summary>
    /// The exception path through the behavior allows activities to catch and handle exceptions
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <typeparam name="TException"></typeparam>
    /// <param name="context"></param>
    /// <returns></returns>
    Task FaultedAsync<T, TException>(BehaviorExceptionContext<TInstance, T, TException> context)
        where T : class
        where TException : Exception;

    /// <summary>
    /// The exception path through the behavior allows activities to catch and handle exceptions
    /// </summary>
    /// <typeparam name="TException"></typeparam>
    /// <param name="context"></param>
    /// <returns></returns>
    Task FaultedAsync<TException>(BehaviorExceptionContext<TInstance, TException> context)
        where TException : Exception;
}


/// <summary>
/// A behavior is a chain of activities invoked by a state
/// </summary>
/// <typeparam name="TSaga">The state type</typeparam>
/// <typeparam name="TMessage">The data type of the behavior</typeparam>
public interface IBehavior<TSaga, in TMessage> :
    IVisitable
    where TSaga : class, SagaStateMachineInstance
    where TMessage : class
{
    /// <summary>
    /// Execute the activity with the given behavior context
    /// </summary>
    /// <param name="context">The behavior context</param>
    /// <returns>An awaitable task</returns>
    Task ExecuteAsync(BehaviorContext<TSaga, TMessage> context);

    /// <summary>
    /// The exception path through the behavior allows activities to catch and handle exceptions
    /// </summary>
    /// <typeparam name="TException"></typeparam>
    /// <param name="context"></param>
    /// <returns></returns>
    Task FaultedAsync<TException>(BehaviorExceptionContext<TSaga, TMessage, TException> context)
        where TException : Exception;
}
