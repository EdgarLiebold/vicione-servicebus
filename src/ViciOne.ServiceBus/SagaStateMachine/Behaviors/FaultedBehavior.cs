using System;
using System.Threading.Tasks;

namespace ViciOne.ServiceBus.SagaStateMachine;

/// <summary>
/// Provides a faulted behavior implementation.
/// </summary>
/// <typeparam name="TSaga">The t saga type.</typeparam>
public class FaultedBehavior<TSaga> :
    IBehavior<TSaga>
    where TSaga : class, SagaStateMachineInstance
{
    /// <summary>
    /// Performs the accept operation.
    /// </summary>
    /// <param name="visitor">The visitor value.</param>
    public void Accept(StateMachineVisitor visitor)
    {
        visitor.Visit(this);
    }

    /// <summary>
    /// Performs the probe operation.
    /// </summary>
    /// <param name="context">The operation context.</param>
    public void Probe(ProbeContext context)
    {
        context.CreateScope("exception");
    }

    /// <summary>
    /// Performs the execute operation.
    /// </summary>
    /// <param name="context">The operation context.</param>
    /// <returns>The result of the operation.</returns>
    public Task ExecuteAsync(BehaviorContext<TSaga> context)
    {
        return Task.CompletedTask;
    }

    /// <summary>
    /// Performs the execute operation.
    /// </summary>
    /// <typeparam name="T">The t type.</typeparam>
    /// <param name="context">The operation context.</param>
    /// <returns>The result of the operation.</returns>
    public Task ExecuteAsync<T>(BehaviorContext<TSaga, T> context)
        where T : class
    {
        return Task.CompletedTask;
    }

    /// <summary>
    /// Performs the faulted operation.
    /// </summary>
    /// <typeparam name="T">The t type.</typeparam>
    /// <typeparam name="TException">The t exception type.</typeparam>
    /// <param name="context">The operation context.</param>
    /// <returns>The result of the operation.</returns>
    public Task FaultedAsync<T, TException>(BehaviorExceptionContext<TSaga, T, TException> context)
        where T : class
        where TException : Exception
    {
        throw new EventExecutionException($"The {context.Event} execution faulted", context.Exception);
    }

    /// <summary>
    /// Performs the faulted operation.
    /// </summary>
    /// <typeparam name="TException">The t exception type.</typeparam>
    /// <param name="context">The operation context.</param>
    /// <returns>The result of the operation.</returns>
    public Task FaultedAsync<TException>(BehaviorExceptionContext<TSaga, TException> context)
        where TException : Exception
    {
        throw new EventExecutionException($"The {context.Event} execution faulted", context.Exception);
    }
}


/// <summary>
/// Provides a faulted behavior implementation.
/// </summary>
/// <typeparam name="TSaga">The t saga type.</typeparam>
/// <typeparam name="TMessage">The t message type.</typeparam>
public class FaultedBehavior<TSaga, TMessage> :
    IBehavior<TSaga, TMessage>
    where TSaga : class, SagaStateMachineInstance
    where TMessage : class
{
    /// <summary>
    /// Performs the accept operation.
    /// </summary>
    /// <param name="visitor">The visitor value.</param>
    public void Accept(StateMachineVisitor visitor)
    {
        visitor.Visit(this);
    }

    /// <summary>
    /// Performs the probe operation.
    /// </summary>
    /// <param name="context">The operation context.</param>
    public void Probe(ProbeContext context)
    {
        context.CreateScope("exception");
    }

    /// <summary>
    /// Performs the execute operation.
    /// </summary>
    /// <param name="context">The operation context.</param>
    /// <returns>The result of the operation.</returns>
    public Task ExecuteAsync(BehaviorContext<TSaga, TMessage> context)
    {
        return Task.CompletedTask;
    }

    /// <summary>
    /// Performs the faulted operation.
    /// </summary>
    /// <typeparam name="TException">The t exception type.</typeparam>
    /// <param name="context">The operation context.</param>
    /// <returns>The result of the operation.</returns>
    public Task FaultedAsync<TException>(BehaviorExceptionContext<TSaga, TMessage, TException> context)
        where TException : Exception
    {
        throw new EventExecutionException($"The {context.Event} execution faulted", context.Exception);
    }
}
