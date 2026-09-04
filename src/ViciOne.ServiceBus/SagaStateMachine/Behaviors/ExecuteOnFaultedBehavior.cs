using System;
using System.Threading.Tasks;

namespace ViciOne.ServiceBus.SagaStateMachine;

/// <summary>
/// Provides an execute on faulted behavior implementation.
/// </summary>
/// <typeparam name="TSaga">The t saga type.</typeparam>
/// <typeparam name="TException">The t exception type.</typeparam>
public class ExecuteOnFaultedBehavior<TSaga, TException> :
    IBehavior<TSaga>
    where TException : Exception
    where TSaga : class, SagaStateMachineInstance
{
    readonly BehaviorExceptionContext<TSaga, TException> _context;
    readonly IBehavior<TSaga> _next;

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="next">The next value.</param>
    /// <param name="context">The operation context.</param>
    public ExecuteOnFaultedBehavior(IBehavior<TSaga> next, BehaviorExceptionContext<TSaga, TException> context)
    {
        _next = next;
        _context = context;
    }

    /// <summary>
    /// Performs the accept operation.
    /// </summary>
    /// <param name="visitor">The visitor value.</param>
    public void Accept(StateMachineVisitor visitor)
    {
        _next.Accept(visitor);
    }

    /// <summary>
    /// Performs the probe operation.
    /// </summary>
    /// <param name="context">The operation context.</param>
    public void Probe(ProbeContext context)
    {
        _next.Probe(context);
    }

    Task IBehavior<TSaga>.ExecuteAsync(BehaviorContext<TSaga> context)
    {
        return _next.FaultedAsync(_context);
    }

    Task IBehavior<TSaga>.ExecuteAsync<T>(BehaviorContext<TSaga, T> context)
    {
        return _next.FaultedAsync(_context);
    }

    Task IBehavior<TSaga>.FaultedAsync<TData, T>(BehaviorExceptionContext<TSaga, TData, T> context)
    {
        throw new SagaStateMachineException("This should not ever be called.");
    }

    Task IBehavior<TSaga>.FaultedAsync<T>(BehaviorExceptionContext<TSaga, T> context)
    {
        throw new SagaStateMachineException("This should not ever be called.");
    }
}


/// <summary>
/// Provides an execute on faulted behavior implementation.
/// </summary>
/// <typeparam name="TSaga">The t saga type.</typeparam>
/// <typeparam name="TMessage">The t message type.</typeparam>
/// <typeparam name="TException">The t exception type.</typeparam>
public class ExecuteOnFaultedBehavior<TSaga, TMessage, TException> :
    IBehavior<TSaga>
    where TException : Exception
    where TSaga : class, SagaStateMachineInstance
    where TMessage : class
{
    readonly BehaviorExceptionContext<TSaga, TMessage, TException> _context;
    readonly IBehavior<TSaga, TMessage> _next;

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="next">The next value.</param>
    /// <param name="context">The operation context.</param>
    public ExecuteOnFaultedBehavior(IBehavior<TSaga, TMessage> next, BehaviorExceptionContext<TSaga, TMessage, TException> context)
    {
        _next = next;
        _context = context;
    }

    /// <summary>
    /// Performs the accept operation.
    /// </summary>
    /// <param name="visitor">The visitor value.</param>
    public void Accept(StateMachineVisitor visitor)
    {
        _next.Accept(visitor);
    }

    /// <summary>
    /// Performs the probe operation.
    /// </summary>
    /// <param name="context">The operation context.</param>
    public void Probe(ProbeContext context)
    {
        _next.Probe(context);
    }

    Task IBehavior<TSaga>.ExecuteAsync(BehaviorContext<TSaga> context)
    {
        return _next.FaultedAsync(_context);
    }

    Task IBehavior<TSaga>.ExecuteAsync<T>(BehaviorContext<TSaga, T> context)
    {
        return _next.FaultedAsync(_context);
    }

    Task IBehavior<TSaga>.FaultedAsync<TD, T>(BehaviorExceptionContext<TSaga, TD, T> context)
    {
        throw new SagaStateMachineException("This should not ever be called.");
    }

    Task IBehavior<TSaga>.FaultedAsync<T>(BehaviorExceptionContext<TSaga, T> context)
    {
        throw new SagaStateMachineException("This should not ever be called.");
    }
}
