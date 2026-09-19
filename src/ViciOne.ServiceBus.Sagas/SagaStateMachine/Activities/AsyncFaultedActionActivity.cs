using System;
using System.Threading.Tasks;

namespace ViciOne.ServiceBus.SagaStateMachine;

/// <summary>Executes the async faulted action activity.</summary>
/// <typeparam name="TSaga">The saga state managed by the member.</typeparam>
/// <typeparam name="TException">The exception handled by the member.</typeparam>
public class AsyncFaultedActionActivity<TSaga, TException> :
    IStateMachineActivity<TSaga>
    where TException : Exception
    where TSaga : class, ISagaStateMachineInstance
{
    readonly Func<IBehaviorExceptionContext<TSaga, TException>, Task> _asyncAction;

    /// <summary>Initializes a new instance.</summary>
    /// <param name="asyncAction">The async action.</param>
    /// <exception cref="ArgumentNullException"><paramref name="asyncAction"/> is <see langword="null"/>.</exception>
    public AsyncFaultedActionActivity(Func<IBehaviorExceptionContext<TSaga, TException>, Task> asyncAction)
    {
        _asyncAction = asyncAction ?? throw new ArgumentNullException(nameof(asyncAction));
    }

    /// <summary>Accepts the supplied value.</summary>
    /// <param name="visitor">The visitor.</param>
    /// <exception cref="ArgumentNullException"><paramref name="visitor"/> is <see langword="null"/>.</exception>
    public void Accept(IStateMachineVisitor visitor)
    {
        ArgumentNullException.ThrowIfNull(visitor);
        visitor.Visit(this);
    }

    /// <summary>Writes diagnostic information to the probe context.</summary>
    /// <param name="context">The context associated with the operation.</param>
    /// <exception cref="ArgumentNullException"><paramref name="context"/> is <see langword="null"/>.</exception>
    public void Probe(ProbeContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        context.CreateScope("then-async-faulted");
    }

    /// <summary>Runs the configured action.</summary>
    /// <param name="context">The context associated with the operation.</param>
    /// <param name="next">The next pipeline stage to invoke.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="context"/> or <paramref name="next"/> is <see langword="null"/>.</exception>
    public Task ExecuteAsync(IBehaviorContext<TSaga> context, IBehavior<TSaga> next)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(next);
        return next.ExecuteAsync(context);
    }

    /// <summary>Runs the configured action.</summary>
    /// <typeparam name="TData">The message contract processed by the operation.</typeparam>
    /// <param name="context">The context associated with the operation.</param>
    /// <param name="next">The next pipeline stage to invoke.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="context"/> or <paramref name="next"/> is <see langword="null"/>.</exception>
    public Task ExecuteAsync<TData>(IBehaviorContext<TSaga, TData> context, IBehavior<TSaga, TData> next)
        where TData : class
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(next);
        return next.ExecuteAsync(context);
    }

    /// <summary>Reports that the operation has faulted.</summary>
    /// <typeparam name="T">The exception reported by the operation.</typeparam>
    /// <param name="context">The context associated with the operation.</param>
    /// <param name="next">The next pipeline stage to invoke.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="context"/> or <paramref name="next"/> is <see langword="null"/>.</exception>
    /// <exception cref="InvalidOperationException">The configured async action returns <see langword="null"/>.</exception>
    public async Task FaultedAsync<T>(IBehaviorExceptionContext<TSaga, T> context, IBehavior<TSaga> next)
        where T : Exception
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(next);
        var exceptionContext = context as IBehaviorExceptionContext<TSaga, TException>;
        if (exceptionContext != null)
        {
            Task actionTask = _asyncAction(exceptionContext)
                ?? throw new InvalidOperationException("The async action returned null.");
            await actionTask.ConfigureAwait(false);
        }

        await next.FaultedAsync(context).ConfigureAwait(false);
    }

    /// <summary>Reports that the operation has faulted.</summary>
    /// <typeparam name="TData">The message contract associated with the fault.</typeparam>
    /// <typeparam name="T">The exception reported by the operation.</typeparam>
    /// <param name="context">The context associated with the operation.</param>
    /// <param name="next">The next pipeline stage to invoke.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="context"/> or <paramref name="next"/> is <see langword="null"/>.</exception>
    /// <exception cref="InvalidOperationException">The configured async action returns <see langword="null"/>.</exception>
    public async Task FaultedAsync<TData, T>(IBehaviorExceptionContext<TSaga, TData, T> context, IBehavior<TSaga, TData> next)
        where TData : class
        where T : Exception
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(next);
        var exceptionContext = context as IBehaviorExceptionContext<TSaga, TData, TException>;
        if (exceptionContext != null)
        {
            Task actionTask = _asyncAction(exceptionContext)
                ?? throw new InvalidOperationException("The async action returned null.");
            await actionTask.ConfigureAwait(false);
        }

        await next.FaultedAsync(context).ConfigureAwait(false);
    }
}


/// <summary>Executes the async faulted action activity.</summary>
/// <typeparam name="TSaga">The saga state managed by the member.</typeparam>
/// <typeparam name="TMessage">The message contract processed by the member.</typeparam>
/// <typeparam name="TException">The exception handled by the member.</typeparam>
public class AsyncFaultedActionActivity<TSaga, TMessage, TException> :
    IStateMachineActivity<TSaga, TMessage>
    where TSaga : class, ISagaStateMachineInstance
    where TException : Exception
    where TMessage : class
{
    readonly Func<IBehaviorExceptionContext<TSaga, TMessage, TException>, Task> _asyncAction;

    /// <summary>Initializes a new instance.</summary>
    /// <param name="asyncAction">The async action.</param>
    /// <exception cref="ArgumentNullException"><paramref name="asyncAction"/> is <see langword="null"/>.</exception>
    public AsyncFaultedActionActivity(Func<IBehaviorExceptionContext<TSaga, TMessage, TException>, Task> asyncAction)
    {
        _asyncAction = asyncAction ?? throw new ArgumentNullException(nameof(asyncAction));
    }

    /// <summary>Accepts the supplied value.</summary>
    /// <param name="visitor">The visitor.</param>
    /// <exception cref="ArgumentNullException"><paramref name="visitor"/> is <see langword="null"/>.</exception>
    public void Accept(IStateMachineVisitor visitor)
    {
        ArgumentNullException.ThrowIfNull(visitor);
        visitor.Visit(this);
    }

    /// <summary>Writes diagnostic information to the probe context.</summary>
    /// <param name="context">The context associated with the operation.</param>
    /// <exception cref="ArgumentNullException"><paramref name="context"/> is <see langword="null"/>.</exception>
    public void Probe(ProbeContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        context.CreateScope("then-async-faulted");
    }

    /// <summary>Runs the configured action.</summary>
    /// <param name="context">The context associated with the operation.</param>
    /// <param name="next">The next pipeline stage to invoke.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="context"/> or <paramref name="next"/> is <see langword="null"/>.</exception>
    public Task ExecuteAsync(IBehaviorContext<TSaga, TMessage> context, IBehavior<TSaga, TMessage> next)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(next);
        return next.ExecuteAsync(context);
    }

    /// <summary>Reports that the operation has faulted.</summary>
    /// <typeparam name="T">The exception reported by the operation.</typeparam>
    /// <param name="context">The context associated with the operation.</param>
    /// <param name="next">The next pipeline stage to invoke.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="context"/> or <paramref name="next"/> is <see langword="null"/>.</exception>
    /// <exception cref="InvalidOperationException">The configured async action returns <see langword="null"/>.</exception>
    public async Task FaultedAsync<T>(IBehaviorExceptionContext<TSaga, TMessage, T> context, IBehavior<TSaga, TMessage> next)
        where T : Exception
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(next);
        var exceptionContext = context as IBehaviorExceptionContext<TSaga, TMessage, TException>;
        if (exceptionContext != null)
        {
            Task actionTask = _asyncAction(exceptionContext)
                ?? throw new InvalidOperationException("The async action returned null.");
            await actionTask.ConfigureAwait(false);
        }

        await next.FaultedAsync(context).ConfigureAwait(false);
    }
}
