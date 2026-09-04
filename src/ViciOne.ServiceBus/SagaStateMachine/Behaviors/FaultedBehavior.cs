using System;
using System.Threading.Tasks;

namespace ViciOne.ServiceBus.SagaStateMachine;

public class FaultedBehavior<TSaga> :
    IBehavior<TSaga>
    where TSaga : class, SagaStateMachineInstance
{
    public void Accept(StateMachineVisitor visitor)
    {
        visitor.Visit(this);
    }

    public void Probe(ProbeContext context)
    {
        context.CreateScope("exception");
    }

    public Task ExecuteAsync(BehaviorContext<TSaga> context)
    {
        return Task.CompletedTask;
    }

    public Task ExecuteAsync<T>(BehaviorContext<TSaga, T> context)
        where T : class
    {
        return Task.CompletedTask;
    }

    public Task FaultedAsync<T, TException>(BehaviorExceptionContext<TSaga, T, TException> context)
        where T : class
        where TException : Exception
    {
        throw new EventExecutionException($"The {context.Event} execution faulted", context.Exception);
    }

    public Task FaultedAsync<TException>(BehaviorExceptionContext<TSaga, TException> context)
        where TException : Exception
    {
        throw new EventExecutionException($"The {context.Event} execution faulted", context.Exception);
    }
}


public class FaultedBehavior<TSaga, TMessage> :
    IBehavior<TSaga, TMessage>
    where TSaga : class, SagaStateMachineInstance
    where TMessage : class
{
    public void Accept(StateMachineVisitor visitor)
    {
        visitor.Visit(this);
    }

    public void Probe(ProbeContext context)
    {
        context.CreateScope("exception");
    }

    public Task ExecuteAsync(BehaviorContext<TSaga, TMessage> context)
    {
        return Task.CompletedTask;
    }

    public Task FaultedAsync<TException>(BehaviorExceptionContext<TSaga, TMessage, TException> context)
        where TException : Exception
    {
        throw new EventExecutionException($"The {context.Event} execution faulted", context.Exception);
    }
}
