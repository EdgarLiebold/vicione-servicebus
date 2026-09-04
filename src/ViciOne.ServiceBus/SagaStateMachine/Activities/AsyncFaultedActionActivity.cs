using System;
using System.Threading.Tasks;

namespace ViciOne.ServiceBus.SagaStateMachine;

public class AsyncFaultedActionActivity<TSaga, TException> :
    IStateMachineActivity<TSaga>
    where TException : Exception
    where TSaga : class, SagaStateMachineInstance
{
    readonly Func<BehaviorExceptionContext<TSaga, TException>, Task> _asyncAction;

    public AsyncFaultedActionActivity(Func<BehaviorExceptionContext<TSaga, TException>, Task> asyncAction)
    {
        _asyncAction = asyncAction;
    }

    public void Accept(StateMachineVisitor visitor)
    {
        visitor.Visit(this);
    }

    public void Probe(ProbeContext context)
    {
        context.CreateScope("then-async-faulted");
    }

    public Task ExecuteAsync(BehaviorContext<TSaga> context, IBehavior<TSaga> next)
    {
        return next.ExecuteAsync(context);
    }

    public Task ExecuteAsync<TData>(BehaviorContext<TSaga, TData> context, IBehavior<TSaga, TData> next)
        where TData : class
    {
        return next.ExecuteAsync(context);
    }

    public async Task FaultedAsync<T>(BehaviorExceptionContext<TSaga, T> context, IBehavior<TSaga> next)
        where T : Exception
    {
        var exceptionContext = context as BehaviorExceptionContext<TSaga, TException>;
        if (exceptionContext != null)
            await _asyncAction(exceptionContext);

        await next.FaultedAsync(context);
    }

    public async Task FaultedAsync<TData, T>(BehaviorExceptionContext<TSaga, TData, T> context, IBehavior<TSaga, TData> next)
        where TData : class
        where T : Exception
    {
        var exceptionContext = context as BehaviorExceptionContext<TSaga, TData, TException>;
        if (exceptionContext != null)
            await _asyncAction(exceptionContext);

        await next.FaultedAsync(context);
    }
}


public class AsyncFaultedActionActivity<TSaga, TMessage, TException> :
    IStateMachineActivity<TSaga, TMessage>
    where TSaga : class, SagaStateMachineInstance
    where TException : Exception
    where TMessage : class
{
    readonly Func<BehaviorExceptionContext<TSaga, TMessage, TException>, Task> _asyncAction;

    public AsyncFaultedActionActivity(Func<BehaviorExceptionContext<TSaga, TMessage, TException>, Task> asyncAction)
    {
        _asyncAction = asyncAction;
    }

    public void Accept(StateMachineVisitor visitor)
    {
        visitor.Visit(this);
    }

    public void Probe(ProbeContext context)
    {
        context.CreateScope("then-async-faulted");
    }

    public Task ExecuteAsync(BehaviorContext<TSaga, TMessage> context, IBehavior<TSaga, TMessage> next)
    {
        return next.ExecuteAsync(context);
    }

    public async Task FaultedAsync<T>(BehaviorExceptionContext<TSaga, TMessage, T> context, IBehavior<TSaga, TMessage> next)
        where T : Exception
    {
        var exceptionContext = context as BehaviorExceptionContext<TSaga, TMessage, TException>;
        if (exceptionContext != null)
            await _asyncAction(exceptionContext);

        await next.FaultedAsync(context);
    }
}
