using System;
using System.Threading.Tasks;

namespace ViciOne.ServiceBus.SagaStateMachine;

/// <summary>
/// Provides an async faulted action activity implementation.
/// </summary>
/// <typeparam name="TSaga">The t saga type.</typeparam>
/// <typeparam name="TException">The t exception type.</typeparam>
public class AsyncFaultedActionActivity<TSaga, TException> :
    IStateMachineActivity<TSaga>
    where TException : Exception
    where TSaga : class, SagaStateMachineInstance
{
    readonly Func<BehaviorExceptionContext<TSaga, TException>, Task> _asyncAction;

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="asyncAction">The async action value.</param>
    public AsyncFaultedActionActivity(Func<BehaviorExceptionContext<TSaga, TException>, Task> asyncAction)
    {
        _asyncAction = asyncAction;
    }

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
        context.CreateScope("then-async-faulted");
    }

    /// <summary>
    /// Performs the execute operation.
    /// </summary>
    /// <param name="context">The operation context.</param>
    /// <param name="next">The next value.</param>
    /// <returns>The result of the operation.</returns>
    public Task ExecuteAsync(BehaviorContext<TSaga> context, IBehavior<TSaga> next)
    {
        return next.ExecuteAsync(context);
    }

    /// <summary>
    /// Performs the execute operation.
    /// </summary>
    /// <typeparam name="TData">The t data type.</typeparam>
    /// <param name="context">The operation context.</param>
    /// <param name="next">The next value.</param>
    /// <returns>The result of the operation.</returns>
    public Task ExecuteAsync<TData>(BehaviorContext<TSaga, TData> context, IBehavior<TSaga, TData> next)
        where TData : class
    {
        return next.ExecuteAsync(context);
    }

    /// <summary>
    /// Performs the faulted operation.
    /// </summary>
    /// <typeparam name="T">The t type.</typeparam>
    /// <param name="context">The operation context.</param>
    /// <param name="next">The next value.</param>
    /// <returns>The result of the operation.</returns>
    public async Task FaultedAsync<T>(BehaviorExceptionContext<TSaga, T> context, IBehavior<TSaga> next)
        where T : Exception
    {
        var exceptionContext = context as BehaviorExceptionContext<TSaga, TException>;
        if (exceptionContext != null)
            await _asyncAction(exceptionContext);

        await next.FaultedAsync(context);
    }

    /// <summary>
    /// Performs the faulted operation.
    /// </summary>
    /// <typeparam name="TData">The t data type.</typeparam>
    /// <typeparam name="T">The t type.</typeparam>
    /// <param name="context">The operation context.</param>
    /// <param name="next">The next value.</param>
    /// <returns>The result of the operation.</returns>
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


/// <summary>
/// Provides an async faulted action activity implementation.
/// </summary>
/// <typeparam name="TSaga">The t saga type.</typeparam>
/// <typeparam name="TMessage">The t message type.</typeparam>
/// <typeparam name="TException">The t exception type.</typeparam>
public class AsyncFaultedActionActivity<TSaga, TMessage, TException> :
    IStateMachineActivity<TSaga, TMessage>
    where TSaga : class, SagaStateMachineInstance
    where TException : Exception
    where TMessage : class
{
    readonly Func<BehaviorExceptionContext<TSaga, TMessage, TException>, Task> _asyncAction;

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="asyncAction">The async action value.</param>
    public AsyncFaultedActionActivity(Func<BehaviorExceptionContext<TSaga, TMessage, TException>, Task> asyncAction)
    {
        _asyncAction = asyncAction;
    }

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
        context.CreateScope("then-async-faulted");
    }

    /// <summary>
    /// Performs the execute operation.
    /// </summary>
    /// <param name="context">The operation context.</param>
    /// <param name="next">The next value.</param>
    /// <returns>The result of the operation.</returns>
    public Task ExecuteAsync(BehaviorContext<TSaga, TMessage> context, IBehavior<TSaga, TMessage> next)
    {
        return next.ExecuteAsync(context);
    }

    /// <summary>
    /// Performs the faulted operation.
    /// </summary>
    /// <typeparam name="T">The t type.</typeparam>
    /// <param name="context">The operation context.</param>
    /// <param name="next">The next value.</param>
    /// <returns>The result of the operation.</returns>
    public async Task FaultedAsync<T>(BehaviorExceptionContext<TSaga, TMessage, T> context, IBehavior<TSaga, TMessage> next)
        where T : Exception
    {
        var exceptionContext = context as BehaviorExceptionContext<TSaga, TMessage, TException>;
        if (exceptionContext != null)
            await _asyncAction(exceptionContext);

        await next.FaultedAsync(context);
    }
}
