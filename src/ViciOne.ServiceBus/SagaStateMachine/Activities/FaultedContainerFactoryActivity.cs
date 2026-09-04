using System;
using System.Threading.Tasks;

namespace ViciOne.ServiceBus.SagaStateMachine;

public class FaultedContainerFactoryActivity<TSaga, TException, TActivity> :
    IStateMachineActivity<TSaga>
    where TActivity : class, IStateMachineActivity<TSaga>
    where TSaga : class, SagaStateMachineInstance
    where TException : Exception
{
    public void Accept(StateMachineVisitor visitor)
    {
        visitor.Visit(this);
    }

    public Task ExecuteAsync(BehaviorContext<TSaga> context, IBehavior<TSaga> next)
    {
        return next.ExecuteAsync(context);
    }

    public Task ExecuteAsync<T>(BehaviorContext<TSaga, T> context, IBehavior<TSaga, T> next)
        where T : class
    {
        return next.ExecuteAsync(context);
    }

    public Task FaultedAsync<TOtherException>(BehaviorExceptionContext<TSaga, TOtherException> context, IBehavior<TSaga> next)
        where TOtherException : Exception
    {
        if (context is BehaviorExceptionContext<TSaga, TException> exceptionContext)
        {
            var activity = context.GetServiceOrCreateInstance<TActivity>();

            return activity.FaultedAsync(exceptionContext, next);
        }

        return next.FaultedAsync(context);
    }

    public Task FaultedAsync<T, TOtherException>(BehaviorExceptionContext<TSaga, T, TOtherException> context, IBehavior<TSaga, T> next)
        where T : class
        where TOtherException : Exception
    {
        if (context is BehaviorExceptionContext<TSaga, T, TException> exceptionContext)
        {
            var activity = context.GetServiceOrCreateInstance<TActivity>();

            return activity.FaultedAsync(exceptionContext, next);
        }

        return next.FaultedAsync(context);
    }

    public void Probe(ProbeContext context)
    {
        context.CreateScope("containerActivityFactory");
    }
}


public class FaultedContainerFactoryActivity<TSaga, TMessage, TException, TActivity> :
    IStateMachineActivity<TSaga, TMessage>
    where TActivity : class, IStateMachineActivity<TSaga, TMessage>
    where TSaga : class, SagaStateMachineInstance
    where TException : Exception
    where TMessage : class
{
    public void Probe(ProbeContext context)
    {
        context.CreateScope("containerActivityFactory");
    }

    public Task ExecuteAsync(BehaviorContext<TSaga, TMessage> context, IBehavior<TSaga, TMessage> next)
    {
        return next.ExecuteAsync(context);
    }

    public Task FaultedAsync<T>(BehaviorExceptionContext<TSaga, TMessage, T> context, IBehavior<TSaga, TMessage> next)
        where T : Exception
    {
        if (context is BehaviorExceptionContext<TSaga, TMessage, TException> exceptionContext)
        {
            var activity = context.GetServiceOrCreateInstance<TActivity>();

            return activity.FaultedAsync(exceptionContext, next);
        }

        return next.FaultedAsync(context);
    }

    public void Accept(StateMachineVisitor visitor)
    {
        visitor.Visit(this);
    }
}
