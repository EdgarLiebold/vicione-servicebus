using System;
using System.Threading.Tasks;

namespace ViciOne.ServiceBus.SagaStateMachine;

/// <summary>Executes the async faulted action activity.</summary>
/// <typeparam name="TSaga">The saga state managed by the member.</typeparam>
/// <typeparam name="TException">The exception handled by the member.</typeparam>
public class AsyncFaultedActionActivity<TSaga, TException> :
    IStateMachineActivity<TSaga>
    where TException : Exception
    where TSaga : class, SagaStateMachineInstance
{
    readonly Func<BehaviorExceptionContext<TSaga, TException>, Task> _asyncAction;

    /// <summary>Initializes a new instance.</summary>
    /// <param name="asyncAction">The async action.</param>
    public AsyncFaultedActionActivity(Func<BehaviorExceptionContext<TSaga, TException>, Task> asyncAction)
    {
        _asyncAction = asyncAction;
    }

    /// <summary>Accepts the supplied value.</summary>
    /// <param name="visitor">The visitor.</param>
    public void Accept(StateMachineVisitor visitor)
    {
        visitor.Visit(this);
    }

    /// <summary>Writes diagnostic information to the probe context.</summary>
    /// <param name="context">The context associated with the operation.</param>
    public void Probe(ProbeContext context)
    {
        context.CreateScope("then-async-faulted");
    }

    /// <summary>Runs the configured action.</summary>
    /// <param name="context">The context associated with the operation.</param>
    /// <param name="next">The next pipeline stage to invoke.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public Task ExecuteAsync(BehaviorContext<TSaga> context, IBehavior<TSaga> next)
    {
        return next.ExecuteAsync(context);
    }

    /// <summary>Runs the configured action.</summary>
    /// <typeparam name="TData">The data type.</typeparam>
    /// <param name="context">The context associated with the operation.</param>
    /// <param name="next">The next pipeline stage to invoke.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public Task ExecuteAsync<TData>(BehaviorContext<TSaga, TData> context, IBehavior<TSaga, TData> next)
        where TData : class
    {
        return next.ExecuteAsync(context);
    }

    /// <summary>Reports that the operation has faulted.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="context">The context associated with the operation.</param>
    /// <param name="next">The next pipeline stage to invoke.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public async Task FaultedAsync<T>(BehaviorExceptionContext<TSaga, T> context, IBehavior<TSaga> next)
        where T : Exception
    {
        var exceptionContext = context as BehaviorExceptionContext<TSaga, TException>;
        if (exceptionContext != null)
            await _asyncAction(exceptionContext);

        await next.FaultedAsync(context);
    }

    /// <summary>Reports that the operation has faulted.</summary>
    /// <typeparam name="TData">The data type.</typeparam>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="context">The context associated with the operation.</param>
    /// <param name="next">The next pipeline stage to invoke.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
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


/// <summary>Executes the async faulted action activity.</summary>
/// <typeparam name="TSaga">The saga state managed by the member.</typeparam>
/// <typeparam name="TMessage">The message contract processed by the member.</typeparam>
/// <typeparam name="TException">The exception handled by the member.</typeparam>
public class AsyncFaultedActionActivity<TSaga, TMessage, TException> :
    IStateMachineActivity<TSaga, TMessage>
    where TSaga : class, SagaStateMachineInstance
    where TException : Exception
    where TMessage : class
{
    readonly Func<BehaviorExceptionContext<TSaga, TMessage, TException>, Task> _asyncAction;

    /// <summary>Initializes a new instance.</summary>
    /// <param name="asyncAction">The async action.</param>
    public AsyncFaultedActionActivity(Func<BehaviorExceptionContext<TSaga, TMessage, TException>, Task> asyncAction)
    {
        _asyncAction = asyncAction;
    }

    /// <summary>Accepts the supplied value.</summary>
    /// <param name="visitor">The visitor.</param>
    public void Accept(StateMachineVisitor visitor)
    {
        visitor.Visit(this);
    }

    /// <summary>Writes diagnostic information to the probe context.</summary>
    /// <param name="context">The context associated with the operation.</param>
    public void Probe(ProbeContext context)
    {
        context.CreateScope("then-async-faulted");
    }

    /// <summary>Runs the configured action.</summary>
    /// <param name="context">The context associated with the operation.</param>
    /// <param name="next">The next pipeline stage to invoke.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public Task ExecuteAsync(BehaviorContext<TSaga, TMessage> context, IBehavior<TSaga, TMessage> next)
    {
        return next.ExecuteAsync(context);
    }

    /// <summary>Reports that the operation has faulted.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="context">The context associated with the operation.</param>
    /// <param name="next">The next pipeline stage to invoke.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public async Task FaultedAsync<T>(BehaviorExceptionContext<TSaga, TMessage, T> context, IBehavior<TSaga, TMessage> next)
        where T : Exception
    {
        var exceptionContext = context as BehaviorExceptionContext<TSaga, TMessage, TException>;
        if (exceptionContext != null)
            await _asyncAction(exceptionContext);

        await next.FaultedAsync(context);
    }
}
