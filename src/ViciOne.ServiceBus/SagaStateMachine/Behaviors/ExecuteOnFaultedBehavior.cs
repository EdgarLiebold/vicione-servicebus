using System;
using System.Threading.Tasks;

namespace ViciOne.ServiceBus.SagaStateMachine;

/// <summary>Executes execute on faulted state-machine behavior.</summary>
/// <typeparam name="TSaga">The saga state managed by the member.</typeparam>
/// <typeparam name="TException">The exception handled by the member.</typeparam>
public class ExecuteOnFaultedBehavior<TSaga, TException> :
    IBehavior<TSaga>
    where TException : Exception
    where TSaga : class, SagaStateMachineInstance
{
    readonly BehaviorExceptionContext<TSaga, TException> _context;
    readonly IBehavior<TSaga> _next;

    /// <summary>Initializes a new instance.</summary>
    /// <param name="next">The next pipeline stage to invoke.</param>
    /// <param name="context">The context associated with the operation.</param>
    public ExecuteOnFaultedBehavior(IBehavior<TSaga> next, BehaviorExceptionContext<TSaga, TException> context)
    {
        _next = next;
        _context = context;
    }

    /// <summary>Accepts the supplied value.</summary>
    /// <param name="visitor">The visitor.</param>
    public void Accept(StateMachineVisitor visitor)
    {
        _next.Accept(visitor);
    }

    /// <summary>Writes diagnostic information to the probe context.</summary>
    /// <param name="context">The context associated with the operation.</param>
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


/// <summary>Executes execute on faulted state-machine behavior.</summary>
/// <typeparam name="TSaga">The saga state managed by the member.</typeparam>
/// <typeparam name="TMessage">The message contract processed by the member.</typeparam>
/// <typeparam name="TException">The exception handled by the member.</typeparam>
public class ExecuteOnFaultedBehavior<TSaga, TMessage, TException> :
    IBehavior<TSaga>
    where TException : Exception
    where TSaga : class, SagaStateMachineInstance
    where TMessage : class
{
    readonly BehaviorExceptionContext<TSaga, TMessage, TException> _context;
    readonly IBehavior<TSaga, TMessage> _next;

    /// <summary>Initializes a new instance.</summary>
    /// <param name="next">The next pipeline stage to invoke.</param>
    /// <param name="context">The context associated with the operation.</param>
    public ExecuteOnFaultedBehavior(IBehavior<TSaga, TMessage> next, BehaviorExceptionContext<TSaga, TMessage, TException> context)
    {
        _next = next;
        _context = context;
    }

    /// <summary>Accepts the supplied value.</summary>
    /// <param name="visitor">The visitor.</param>
    public void Accept(StateMachineVisitor visitor)
    {
        _next.Accept(visitor);
    }

    /// <summary>Writes diagnostic information to the probe context.</summary>
    /// <param name="context">The context associated with the operation.</param>
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
