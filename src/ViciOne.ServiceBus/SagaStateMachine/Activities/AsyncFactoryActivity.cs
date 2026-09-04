using System;
using System.Threading.Tasks;

namespace ViciOne.ServiceBus.SagaStateMachine;

/// <summary>
/// Provides an async factory activity implementation.
/// </summary>
/// <typeparam name="TSaga">The t saga type.</typeparam>
public class AsyncFactoryActivity<TSaga> :
    IStateMachineActivity<TSaga>
    where TSaga : class, SagaStateMachineInstance
{
    readonly Func<BehaviorContext<TSaga>, Task<IStateMachineActivity<TSaga>>> _activityFactory;

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="activityFactory">The activity factory value.</param>
    public AsyncFactoryActivity(Func<BehaviorContext<TSaga>, Task<IStateMachineActivity<TSaga>>> activityFactory)
    {
        _activityFactory = activityFactory;
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
        context.CreateScope("activityFactory");
    }

    async Task IStateMachineActivity<TSaga>.ExecuteAsync(BehaviorContext<TSaga> context, IBehavior<TSaga> next)
    {
        IStateMachineActivity<TSaga> activity = await _activityFactory(context).ConfigureAwait(false);

        await activity.ExecuteAsync(context, next).ConfigureAwait(false);
    }

    async Task IStateMachineActivity<TSaga>.ExecuteAsync<T>(BehaviorContext<TSaga, T> context, IBehavior<TSaga, T> next)
    {
        IStateMachineActivity<TSaga> activity = await _activityFactory(context).ConfigureAwait(false);

        await activity.ExecuteAsync(context, next).ConfigureAwait(false);
    }

    async Task IStateMachineActivity<TSaga>.FaultedAsync<TException>(BehaviorExceptionContext<TSaga, TException> context, IBehavior<TSaga> next)
    {
        IStateMachineActivity<TSaga> activity = await _activityFactory(context).ConfigureAwait(false);

        await activity.FaultedAsync(context, next).ConfigureAwait(false);
    }

    async Task IStateMachineActivity<TSaga>.FaultedAsync<T, TException>(BehaviorExceptionContext<TSaga, T, TException> context,
        IBehavior<TSaga, T> next)
    {
        IStateMachineActivity<TSaga> activity = await _activityFactory(context).ConfigureAwait(false);

        await activity.FaultedAsync(context, next).ConfigureAwait(false);
    }
}


/// <summary>
/// Provides an async factory activity implementation.
/// </summary>
/// <typeparam name="TSaga">The t saga type.</typeparam>
/// <typeparam name="TMessage">The t message type.</typeparam>
public class AsyncFactoryActivity<TSaga, TMessage> :
    IStateMachineActivity<TSaga, TMessage>
    where TSaga : class, SagaStateMachineInstance
    where TMessage : class
{
    readonly Func<BehaviorContext<TSaga, TMessage>, Task<IStateMachineActivity<TSaga, TMessage>>> _activityFactory;

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="activityFactory">The activity factory value.</param>
    public AsyncFactoryActivity(Func<BehaviorContext<TSaga, TMessage>, Task<IStateMachineActivity<TSaga, TMessage>>> activityFactory)
    {
        _activityFactory = activityFactory;
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
        context.CreateScope("activityFactory");
    }

    /// <summary>
    /// Performs the execute operation.
    /// </summary>
    /// <param name="context">The operation context.</param>
    /// <param name="next">The next value.</param>
    /// <returns>The result of the operation.</returns>
    public async Task ExecuteAsync(BehaviorContext<TSaga, TMessage> context, IBehavior<TSaga, TMessage> next)
    {
        IStateMachineActivity<TSaga, TMessage> activity = await _activityFactory(context).ConfigureAwait(false);

        await activity.ExecuteAsync(context, next).ConfigureAwait(false);
    }

    /// <summary>
    /// Performs the faulted operation.
    /// </summary>
    /// <typeparam name="TException">The t exception type.</typeparam>
    /// <param name="context">The operation context.</param>
    /// <param name="next">The next value.</param>
    /// <returns>The result of the operation.</returns>
    public async Task FaultedAsync<TException>(BehaviorExceptionContext<TSaga, TMessage, TException> context, IBehavior<TSaga, TMessage> next)
        where TException : Exception
    {
        IStateMachineActivity<TSaga, TMessage> activity = await _activityFactory(context).ConfigureAwait(false);

        await activity.FaultedAsync(context, next).ConfigureAwait(false);
    }
}
