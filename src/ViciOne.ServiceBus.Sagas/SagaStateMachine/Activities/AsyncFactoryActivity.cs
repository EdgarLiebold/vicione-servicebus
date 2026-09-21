using System;
using System.Threading.Tasks;

namespace ViciOne.ServiceBus.SagaStateMachine;

/// <summary>Resolves an activity asynchronously for each untyped saga behavior invocation.</summary>
/// <typeparam name="TSaga">The saga instance supplied to the activity factory.</typeparam>
public class AsyncFactoryActivity<TSaga> :
    IStateMachineActivity<TSaga>
    where TSaga : class, ISagaStateMachineInstance
{
    readonly Func<IBehaviorContext<TSaga>, Task<IStateMachineActivity<TSaga>>> _activityFactory;

    /// <summary>Creates a delegating activity with a context-dependent asynchronous factory.</summary>
    /// <param name="activityFactory">The factory that selects the activity for each behavior context.</param>
    /// <exception cref="ArgumentNullException"><paramref name="activityFactory" /> is <see langword="null" />.</exception>
    public AsyncFactoryActivity(Func<IBehaviorContext<TSaga>, Task<IStateMachineActivity<TSaga>>> activityFactory)
    {
        _activityFactory = activityFactory ?? throw new ArgumentNullException(nameof(activityFactory));
    }

    /// <summary>Visits this factory activity in the state-machine graph.</summary>
    /// <param name="visitor">The visitor inspecting the graph.</param>
    /// <exception cref="ArgumentNullException"><paramref name="visitor" /> is <see langword="null" />.</exception>
    public void Accept(IStateMachineVisitor visitor)
    {
        ArgumentNullException.ThrowIfNull(visitor);

        visitor.Visit(this);
    }

    /// <summary>Creates an activity-factory scope in the diagnostic probe.</summary>
    /// <param name="context">The diagnostic probe to extend.</param>
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

        IStateMachineActivity<TSaga> activity = await GetActivityAsync(context).ConfigureAwait(false);

        await activity.ExecuteAsync(context, next).ConfigureAwait(false);
    }

    /// <inheritdoc />
    /// <exception cref="ArgumentNullException"><paramref name="context" /> or <paramref name="next" /> is <see langword="null" />.</exception>
    /// <exception cref="InvalidOperationException">The activity factory returns <see langword="null" />.</exception>
    async Task IStateMachineActivity<TSaga>.ExecuteAsync<T>(IBehaviorContext<TSaga, T> context, IBehavior<TSaga, T> next)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(next);

        IStateMachineActivity<TSaga> activity = await GetActivityAsync(context).ConfigureAwait(false);

        await activity.ExecuteAsync(context, next).ConfigureAwait(false);
    }

    /// <inheritdoc />
    /// <exception cref="ArgumentNullException"><paramref name="context" /> or <paramref name="next" /> is <see langword="null" />.</exception>
    /// <exception cref="InvalidOperationException">The activity factory returns <see langword="null" />.</exception>
    async Task IStateMachineActivity<TSaga>.FaultedAsync<TException>(IBehaviorExceptionContext<TSaga, TException> context, IBehavior<TSaga> next)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(next);

        IStateMachineActivity<TSaga> activity = await GetActivityAsync(context).ConfigureAwait(false);

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

        IStateMachineActivity<TSaga> activity = await GetActivityAsync(context).ConfigureAwait(false);

        await activity.FaultedAsync(context, next).ConfigureAwait(false);
    }

    async Task<IStateMachineActivity<TSaga>> GetActivityAsync(IBehaviorContext<TSaga> context)
    {
        Task<IStateMachineActivity<TSaga>> activityTask = _activityFactory(context)
            ?? throw new InvalidOperationException("The activity factory returned null.");

        return await activityTask.ConfigureAwait(false)
            ?? throw new InvalidOperationException("The activity factory returned null.");
    }
}


/// <summary>Resolves an activity asynchronously for each message-specific saga behavior invocation.</summary>
/// <typeparam name="TSaga">The saga instance supplied to the activity factory.</typeparam>
/// <typeparam name="TMessage">The message contract carried by the behavior context.</typeparam>
public class AsyncFactoryActivity<TSaga, TMessage> :
    IStateMachineActivity<TSaga, TMessage>
    where TSaga : class, ISagaStateMachineInstance
    where TMessage : class
{
    readonly Func<IBehaviorContext<TSaga, TMessage>, Task<IStateMachineActivity<TSaga, TMessage>>> _activityFactory;

    /// <summary>Creates a delegating activity with a message-context-dependent asynchronous factory.</summary>
    /// <param name="activityFactory">The factory that selects an activity for each message behavior context.</param>
    /// <exception cref="ArgumentNullException"><paramref name="activityFactory" /> is <see langword="null" />.</exception>
    public AsyncFactoryActivity(Func<IBehaviorContext<TSaga, TMessage>, Task<IStateMachineActivity<TSaga, TMessage>>> activityFactory)
    {
        _activityFactory = activityFactory ?? throw new ArgumentNullException(nameof(activityFactory));
    }

    /// <summary>Visits this message-specific factory activity in the state-machine graph.</summary>
    /// <param name="visitor">The visitor inspecting the graph.</param>
    /// <exception cref="ArgumentNullException"><paramref name="visitor" /> is <see langword="null" />.</exception>
    public void Accept(IStateMachineVisitor visitor)
    {
        ArgumentNullException.ThrowIfNull(visitor);

        visitor.Visit(this);
    }

    /// <summary>Creates an activity-factory scope in the diagnostic probe.</summary>
    /// <param name="context">The diagnostic probe to extend.</param>
    /// <exception cref="ArgumentNullException"><paramref name="context" /> is <see langword="null" />.</exception>
    public void Probe(ProbeContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        context.CreateScope("activityFactory");
    }

    /// <summary>Resolves and executes the activity selected for this saga message.</summary>
    /// <param name="context">The current saga and message behavior context.</param>
    /// <param name="next">The behavior invoked by the selected activity.</param>
    /// <returns>A task that completes after the selected activity executes.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="context" /> or <paramref name="next" /> is <see langword="null" />.</exception>
    /// <exception cref="InvalidOperationException">The activity factory returns <see langword="null" />.</exception>
    public async Task ExecuteAsync(IBehaviorContext<TSaga, TMessage> context, IBehavior<TSaga, TMessage> next)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(next);

        IStateMachineActivity<TSaga, TMessage> activity = await GetActivityAsync(context).ConfigureAwait(false);

        await activity.ExecuteAsync(context, next).ConfigureAwait(false);
    }

    /// <summary>Resolves and invokes the fault behavior of the selected activity.</summary>
    /// <typeparam name="TException">The exception type carried by the fault context.</typeparam>
    /// <param name="context">The current saga, message, and exception context.</param>
    /// <param name="next">The fault behavior invoked by the selected activity.</param>
    /// <returns>A task that completes after the selected activity handles the fault.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="context" /> or <paramref name="next" /> is <see langword="null" />.</exception>
    /// <exception cref="InvalidOperationException">The activity factory returns <see langword="null" />.</exception>
    public async Task FaultedAsync<TException>(IBehaviorExceptionContext<TSaga, TMessage, TException> context, IBehavior<TSaga, TMessage> next)
        where TException : Exception
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(next);

        IStateMachineActivity<TSaga, TMessage> activity = await GetActivityAsync(context).ConfigureAwait(false);

        await activity.FaultedAsync(context, next).ConfigureAwait(false);
    }

    async Task<IStateMachineActivity<TSaga, TMessage>> GetActivityAsync(IBehaviorContext<TSaga, TMessage> context)
    {
        Task<IStateMachineActivity<TSaga, TMessage>> activityTask = _activityFactory(context)
            ?? throw new InvalidOperationException("The activity factory returned null.");

        return await activityTask.ConfigureAwait(false)
            ?? throw new InvalidOperationException("The activity factory returned null.");
    }
}
