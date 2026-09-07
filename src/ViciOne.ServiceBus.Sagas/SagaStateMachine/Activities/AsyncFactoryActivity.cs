using System;
using System.Threading.Tasks;

namespace ViciOne.ServiceBus.SagaStateMachine;

/// <summary>Executes the async factory activity.</summary>
/// <typeparam name="TSaga">The saga state managed by the member.</typeparam>
public class AsyncFactoryActivity<TSaga> :
    IStateMachineActivity<TSaga>
    where TSaga : class, SagaStateMachineInstance
{
    readonly Func<BehaviorContext<TSaga>, Task<IStateMachineActivity<TSaga>>> _activityFactory;

    /// <summary>Initializes a new instance.</summary>
    /// <param name="activityFactory">The activity factory.</param>
    public AsyncFactoryActivity(Func<BehaviorContext<TSaga>, Task<IStateMachineActivity<TSaga>>> activityFactory)
    {
        _activityFactory = activityFactory;
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


/// <summary>Executes the async factory activity.</summary>
/// <typeparam name="TSaga">The saga state managed by the member.</typeparam>
/// <typeparam name="TMessage">The message contract processed by the member.</typeparam>
public class AsyncFactoryActivity<TSaga, TMessage> :
    IStateMachineActivity<TSaga, TMessage>
    where TSaga : class, SagaStateMachineInstance
    where TMessage : class
{
    readonly Func<BehaviorContext<TSaga, TMessage>, Task<IStateMachineActivity<TSaga, TMessage>>> _activityFactory;

    /// <summary>Initializes a new instance.</summary>
    /// <param name="activityFactory">The activity factory.</param>
    public AsyncFactoryActivity(Func<BehaviorContext<TSaga, TMessage>, Task<IStateMachineActivity<TSaga, TMessage>>> activityFactory)
    {
        _activityFactory = activityFactory;
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
        context.CreateScope("activityFactory");
    }

    /// <summary>Runs the configured action.</summary>
    /// <param name="context">The context associated with the operation.</param>
    /// <param name="next">The next pipeline stage to invoke.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public async Task ExecuteAsync(BehaviorContext<TSaga, TMessage> context, IBehavior<TSaga, TMessage> next)
    {
        IStateMachineActivity<TSaga, TMessage> activity = await _activityFactory(context).ConfigureAwait(false);

        await activity.ExecuteAsync(context, next).ConfigureAwait(false);
    }

    /// <summary>Reports that the operation has faulted.</summary>
    /// <typeparam name="TException">The exception handled by the member.</typeparam>
    /// <param name="context">The context associated with the operation.</param>
    /// <param name="next">The next pipeline stage to invoke.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public async Task FaultedAsync<TException>(BehaviorExceptionContext<TSaga, TMessage, TException> context, IBehavior<TSaga, TMessage> next)
        where TException : Exception
    {
        IStateMachineActivity<TSaga, TMessage> activity = await _activityFactory(context).ConfigureAwait(false);

        await activity.FaultedAsync(context, next).ConfigureAwait(false);
    }
}
