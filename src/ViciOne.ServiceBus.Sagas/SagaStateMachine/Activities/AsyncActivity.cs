using System;
using System.Threading.Tasks;

namespace ViciOne.ServiceBus.SagaStateMachine;

/// <summary>Executes the async activity.</summary>
/// <typeparam name="TSaga">The saga state managed by the member.</typeparam>
public class AsyncActivity<TSaga> :
    IStateMachineActivity<TSaga>
    where TSaga : class, ISagaStateMachineInstance
{
    readonly Func<IBehaviorContext<TSaga>, Task> _asyncAction;

    /// <summary>Initializes a new instance.</summary>
    /// <param name="asyncAction">The async action.</param>
    /// <exception cref="ArgumentNullException"><paramref name="asyncAction"/> is <see langword="null"/>.</exception>
    public AsyncActivity(Func<IBehaviorContext<TSaga>, Task> asyncAction)
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
        context.CreateScope("thenAsync");
    }

    /// <summary>Runs the configured action.</summary>
    /// <param name="context">The context associated with the operation.</param>
    /// <param name="next">The next pipeline stage to invoke.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="context"/> or <paramref name="next"/> is <see langword="null"/>.</exception>
    /// <exception cref="InvalidOperationException">The configured async action returns <see langword="null"/>.</exception>
    public async Task ExecuteAsync(IBehaviorContext<TSaga> context, IBehavior<TSaga> next)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(next);
        Task actionTask = _asyncAction(context)
            ?? throw new InvalidOperationException("The async action returned null.");
        await actionTask.ConfigureAwait(false);

        await next.ExecuteAsync(context).ConfigureAwait(false);
    }

    /// <summary>Runs the configured action.</summary>
    /// <typeparam name="TData">The message contract processed by the operation.</typeparam>
    /// <param name="context">The context associated with the operation.</param>
    /// <param name="next">The next pipeline stage to invoke.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="context"/> or <paramref name="next"/> is <see langword="null"/>.</exception>
    /// <exception cref="InvalidOperationException">The configured async action returns <see langword="null"/>.</exception>
    public async Task ExecuteAsync<TData>(IBehaviorContext<TSaga, TData> context, IBehavior<TSaga, TData> next)
        where TData : class
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(next);
        Task actionTask = _asyncAction(context)
            ?? throw new InvalidOperationException("The async action returned null.");
        await actionTask.ConfigureAwait(false);

        await next.ExecuteAsync(context).ConfigureAwait(false);
    }

    /// <summary>Reports that the operation has faulted.</summary>
    /// <typeparam name="TException">The exception handled by the member.</typeparam>
    /// <param name="context">The context associated with the operation.</param>
    /// <param name="next">The next pipeline stage to invoke.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="context"/> or <paramref name="next"/> is <see langword="null"/>.</exception>
    public Task FaultedAsync<TException>(IBehaviorExceptionContext<TSaga, TException> context, IBehavior<TSaga> next)
        where TException : Exception
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(next);
        return next.FaultedAsync(context);
    }

    /// <summary>Reports that the operation has faulted.</summary>
    /// <typeparam name="T">The message contract associated with the fault.</typeparam>
    /// <typeparam name="TException">The exception reported by the operation.</typeparam>
    /// <param name="context">The context associated with the operation.</param>
    /// <param name="next">The next pipeline stage to invoke.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="context"/> or <paramref name="next"/> is <see langword="null"/>.</exception>
    public Task FaultedAsync<T, TException>(IBehaviorExceptionContext<TSaga, T, TException> context, IBehavior<TSaga, T> next)
        where T : class
        where TException : Exception
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(next);
        return next.FaultedAsync(context);
    }
}


/// <summary>Executes the async activity.</summary>
/// <typeparam name="TSaga">The saga state managed by the member.</typeparam>
/// <typeparam name="TMessage">The message contract processed by the member.</typeparam>
public class AsyncActivity<TSaga, TMessage> :
    IStateMachineActivity<TSaga, TMessage>
    where TSaga : class, ISagaStateMachineInstance
    where TMessage : class
{
    readonly Func<IBehaviorContext<TSaga, TMessage>, Task> _asyncAction;

    /// <summary>Initializes a new instance.</summary>
    /// <param name="asyncAction">The async action.</param>
    /// <exception cref="ArgumentNullException"><paramref name="asyncAction"/> is <see langword="null"/>.</exception>
    public AsyncActivity(Func<IBehaviorContext<TSaga, TMessage>, Task> asyncAction)
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
        context.CreateScope("thenAsync");
    }

    /// <summary>Runs the configured action.</summary>
    /// <param name="context">The context associated with the operation.</param>
    /// <param name="next">The next pipeline stage to invoke.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="context"/> or <paramref name="next"/> is <see langword="null"/>.</exception>
    /// <exception cref="InvalidOperationException">The configured async action returns <see langword="null"/>.</exception>
    public async Task ExecuteAsync(IBehaviorContext<TSaga, TMessage> context, IBehavior<TSaga, TMessage> next)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(next);
        Task actionTask = _asyncAction(context)
            ?? throw new InvalidOperationException("The async action returned null.");
        await actionTask.ConfigureAwait(false);

        await next.ExecuteAsync(context).ConfigureAwait(false);
    }

    /// <summary>Reports that the operation has faulted.</summary>
    /// <typeparam name="TException">The exception handled by the member.</typeparam>
    /// <param name="context">The context associated with the operation.</param>
    /// <param name="next">The next pipeline stage to invoke.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="context"/> or <paramref name="next"/> is <see langword="null"/>.</exception>
    public Task FaultedAsync<TException>(IBehaviorExceptionContext<TSaga, TMessage, TException> context, IBehavior<TSaga, TMessage> next)
        where TException : Exception
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(next);
        return next.FaultedAsync(context);
    }
}
