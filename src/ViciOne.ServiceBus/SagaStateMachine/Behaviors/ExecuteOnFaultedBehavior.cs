using System;
using System.Threading.Tasks;

namespace ViciOne.ServiceBus.SagaStateMachine;

public class ExecuteOnFaultedBehavior<TSaga, TException> :
    IBehavior<TSaga>
    where TException : Exception
    where TSaga : class, SagaStateMachineInstance
{
    readonly BehaviorExceptionContext<TSaga, TException> _context;
    readonly IBehavior<TSaga> _next;

    public ExecuteOnFaultedBehavior(IBehavior<TSaga> next, BehaviorExceptionContext<TSaga, TException> context)
    {
        _next = next;
        _context = context;
    }

    public void Accept(StateMachineVisitor visitor)
    {
        _next.Accept(visitor);
    }

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


public class ExecuteOnFaultedBehavior<TSaga, TMessage, TException> :
    IBehavior<TSaga>
    where TException : Exception
    where TSaga : class, SagaStateMachineInstance
    where TMessage : class
{
    readonly BehaviorExceptionContext<TSaga, TMessage, TException> _context;
    readonly IBehavior<TSaga, TMessage> _next;

    public ExecuteOnFaultedBehavior(IBehavior<TSaga, TMessage> next, BehaviorExceptionContext<TSaga, TMessage, TException> context)
    {
        _next = next;
        _context = context;
    }

    public void Accept(StateMachineVisitor visitor)
    {
        _next.Accept(visitor);
    }

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
