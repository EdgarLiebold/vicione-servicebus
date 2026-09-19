using System;
using System.Threading.Tasks;

namespace ViciOne.ServiceBus.SagaStateMachine;

/// <summary>Executes the async factory activity.</summary>
/// <typeparam name="TSaga">The saga state managed by the member.</typeparam>
public class AsyncFactoryActivity<TSaga> :
    IStateMachineActivity<TSaga>
    where TSaga : class, ISagaStateMachineInstance
{
    readonly Func<IBehaviorContext<TSaga>, Task<IStateMachineActivity<TSaga>>> _activityFactory;

    /// <summary>Initializes a new instance.</summary>
    /// <param name="activityFactory">The activity factory.</param>
    /// <exception cref="ArgumentNullException"><paramref name="activityFactory" /> is <see langword="null" />.</exception>
    public AsyncFactoryActivity(Func<IBehaviorContext<TSaga>, Task<IStateMachineActivity<TSaga>>> activityFactory)
    {
        _activityFactory = activityFactory ?? throw new ArgumentNullException(nameof(activityFactory));
    }

    /// <summary>Accepts the supplied value.</summary>
    /// <param name="visitor">The visitor.</param>
    /// <exception cref="ArgumentNullException"><paramref name="visitor" /> is <see langword="null" />.</exception>
    public void Accept(IStateMachineVisitor visitor)
    {
        ArgumentNullException.ThrowIfNull(visitor);

        visitor.Visit(this);
    }

    /// <summary>Writes diagnostic information to the probe context.</summary>
    /// <param name="context">The context associated with the operation.</param>
    /// <exception cref="ArgumentNullException"><paramref name="context" /> is <see langword="null" />.</exception>
    public void Probe(ProbeContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        context.CreateScope("activityFactory");
    }

    /// <inheritdoc />
    /// <exception cref="ArgumentNullException"><paramref name="context" /> or <paramref name="next" /> is <see langword="null" />.</exception>
    /// <exception cref="InvalidOperationException">The activity factory returns <see langword="null" />.</exception>
    async Task IStateMachineActivity<TSaga>.ExecuteAsync(IBehaviorContext<TSaga> context, IBehavior<TSaga> next)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(next);

        IStateMachineActivity<TSaga> activity = await GetActivity(context).ConfigureAwait(false);

        await activity.ExecuteAsync(context, next).ConfigureAwait(false);
    }

    /// <inheritdoc />
    /// <exception cref="ArgumentNullException"><paramref name="context" /> or <paramref name="next" /> is <see langword="null" />.</exception>
    /// <exception cref="InvalidOperationException">The activity factory returns <see langword="null" />.</exception>
    async Task IStateMachineActivity<TSaga>.ExecuteAsync<T>(IBehaviorContext<TSaga, T> context, IBehavior<TSaga, T> next)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(next);

        IStateMachineActivity<TSaga> activity = await GetActivity(context).ConfigureAwait(false);

        await activity.ExecuteAsync(context, next).ConfigureAwait(false);
    }

    /// <inheritdoc />
    /// <exception cref="ArgumentNullException"><paramref name="context" /> or <paramref name="next" /> is <see langword="null" />.</exception>
    /// <exception cref="InvalidOperationException">The activity factory returns <see langword="null" />.</exception>
    async Task IStateMachineActivity<TSaga>.FaultedAsync<TException>(IBehaviorExceptionContext<TSaga, TException> context, IBehavior<TSaga> next)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(next);

        IStateMachineActivity<TSaga> activity = await GetActivity(context).ConfigureAwait(false);

        await activity.FaultedAsync(context, next).ConfigureAwait(false);
    }

    /// <inheritdoc />
    /// <exception cref="ArgumentNullException"><paramref name="context" /> or <paramref name="next" /> is <see langword="null" />.</exception>
    /// <exception cref="InvalidOperationException">The activity factory returns <see langword="null" />.</exception>
    async Task IStateMachineActivity<TSaga>.FaultedAsync<T, TException>(IBehaviorExceptionContext<TSaga, T, TException> context,
        IBehavior<TSaga, T> next)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(next);

        IStateMachineActivity<TSaga> activity = await GetActivity(context).ConfigureAwait(false);

        await activity.FaultedAsync(context, next).ConfigureAwait(false);
    }

    async Task<IStateMachineActivity<TSaga>> GetActivity(IBehaviorContext<TSaga> context)
    {
        Task<IStateMachineActivity<TSaga>> activityTask = _activityFactory(context)
            ?? throw new InvalidOperationException("The activity factory returned null.");

        return await activityTask.ConfigureAwait(false)
            ?? throw new InvalidOperationException("The activity factory returned null.");
    }
}


/// <summary>Executes the async factory activity.</summary>
/// <typeparam name="TSaga">The saga state managed by the member.</typeparam>
/// <typeparam name="TMessage">The message contract processed by the member.</typeparam>
public class AsyncFactoryActivity<TSaga, TMessage> :
    IStateMachineActivity<TSaga, TMessage>
    where TSaga : class, ISagaStateMachineInstance
    where TMessage : class
{
    readonly Func<IBehaviorContext<TSaga, TMessage>, Task<IStateMachineActivity<TSaga, TMessage>>> _activityFactory;

    /// <summary>Initializes a new instance.</summary>
    /// <param name="activityFactory">The activity factory.</param>
    /// <exception cref="ArgumentNullException"><paramref name="activityFactory" /> is <see langword="null" />.</exception>
    public AsyncFactoryActivity(Func<IBehaviorContext<TSaga, TMessage>, Task<IStateMachineActivity<TSaga, TMessage>>> activityFactory)
    {
        _activityFactory = activityFactory ?? throw new ArgumentNullException(nameof(activityFactory));
    }

    /// <summary>Accepts the supplied value.</summary>
    /// <param name="visitor">The visitor.</param>
    /// <exception cref="ArgumentNullException"><paramref name="visitor" /> is <see langword="null" />.</exception>
    public void Accept(IStateMachineVisitor visitor)
    {
        ArgumentNullException.ThrowIfNull(visitor);

        visitor.Visit(this);
    }

    /// <summary>Writes diagnostic information to the probe context.</summary>
    /// <param name="context">The context associated with the operation.</param>
    /// <exception cref="ArgumentNullException"><paramref name="context" /> is <see langword="null" />.</exception>
    public void Probe(ProbeContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        context.CreateScope("activityFactory");
    }

    /// <summary>Runs the configured action.</summary>
    /// <param name="context">The context associated with the operation.</param>
    /// <param name="next">The next pipeline stage to invoke.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="context" /> or <paramref name="next" /> is <see langword="null" />.</exception>
    /// <exception cref="InvalidOperationException">The activity factory returns <see langword="null" />.</exception>
    public async Task ExecuteAsync(IBehaviorContext<TSaga, TMessage> context, IBehavior<TSaga, TMessage> next)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(next);

        IStateMachineActivity<TSaga, TMessage> activity = await GetActivity(context).ConfigureAwait(false);

        await activity.ExecuteAsync(context, next).ConfigureAwait(false);
    }

    /// <summary>Reports that the operation has faulted.</summary>
    /// <typeparam name="TException">The exception handled by the member.</typeparam>
    /// <param name="context">The context associated with the operation.</param>
    /// <param name="next">The next pipeline stage to invoke.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="context" /> or <paramref name="next" /> is <see langword="null" />.</exception>
    /// <exception cref="InvalidOperationException">The activity factory returns <see langword="null" />.</exception>
    public async Task FaultedAsync<TException>(IBehaviorExceptionContext<TSaga, TMessage, TException> context, IBehavior<TSaga, TMessage> next)
        where TException : Exception
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(next);

        IStateMachineActivity<TSaga, TMessage> activity = await GetActivity(context).ConfigureAwait(false);

        await activity.FaultedAsync(context, next).ConfigureAwait(false);
    }

    async Task<IStateMachineActivity<TSaga, TMessage>> GetActivity(IBehaviorContext<TSaga, TMessage> context)
    {
        Task<IStateMachineActivity<TSaga, TMessage>> activityTask = _activityFactory(context)
            ?? throw new InvalidOperationException("The activity factory returned null.");

        return await activityTask.ConfigureAwait(false)
            ?? throw new InvalidOperationException("The activity factory returned null.");
    }
}
