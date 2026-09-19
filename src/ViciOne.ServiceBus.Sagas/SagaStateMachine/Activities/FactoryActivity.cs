using System;
using System.Threading.Tasks;

namespace ViciOne.ServiceBus.SagaStateMachine;

/// <summary>Executes the factory activity.</summary>
/// <typeparam name="TSaga">The saga state managed by the member.</typeparam>
public class FactoryActivity<TSaga> :
    IStateMachineActivity<TSaga>
    where TSaga : class, ISagaStateMachineInstance
{
    readonly Func<IBehaviorContext<TSaga>, IStateMachineActivity<TSaga>> _activityFactory;

    /// <summary>Initializes a new instance.</summary>
    /// <param name="activityFactory">The activity factory.</param>
    /// <exception cref="ArgumentNullException"><paramref name="activityFactory" /> is <see langword="null" />.</exception>
    public FactoryActivity(Func<IBehaviorContext<TSaga>, IStateMachineActivity<TSaga>> activityFactory)
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

        context.CreateScope("factory");
    }

    /// <summary>Runs the configured action.</summary>
    /// <param name="context">The context associated with the operation.</param>
    /// <param name="next">The next pipeline stage to invoke.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="context" /> or <paramref name="next" /> is <see langword="null" />.</exception>
    /// <exception cref="InvalidOperationException">The activity factory returns <see langword="null" />.</exception>
    public Task ExecuteAsync(IBehaviorContext<TSaga> context, IBehavior<TSaga> next)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(next);

        IStateMachineActivity<TSaga> activity = GetActivity(context);

        return activity.ExecuteAsync(context, next);
    }

    /// <summary>Runs the configured action.</summary>
    /// <typeparam name="T">The message contract type.</typeparam>
    /// <param name="context">The context associated with the operation.</param>
    /// <param name="next">The next pipeline stage to invoke.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="context" /> or <paramref name="next" /> is <see langword="null" />.</exception>
    /// <exception cref="InvalidOperationException">The activity factory returns <see langword="null" />.</exception>
    public Task ExecuteAsync<T>(IBehaviorContext<TSaga, T> context, IBehavior<TSaga, T> next)
        where T : class
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(next);

        IStateMachineActivity<TSaga> activity = GetActivity(context);

        return activity.ExecuteAsync(context, next);
    }

    /// <summary>Reports that the operation has faulted.</summary>
    /// <typeparam name="TException">The exception handled by the member.</typeparam>
    /// <param name="context">The context associated with the operation.</param>
    /// <param name="next">The next pipeline stage to invoke.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="context" /> or <paramref name="next" /> is <see langword="null" />.</exception>
    /// <exception cref="InvalidOperationException">The activity factory returns <see langword="null" />.</exception>
    public Task FaultedAsync<TException>(IBehaviorExceptionContext<TSaga, TException> context, IBehavior<TSaga> next)
        where TException : Exception
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(next);

        IStateMachineActivity<TSaga> activity = GetActivity(context);

        return activity.FaultedAsync(context, next);
    }

    /// <summary>Reports that the operation has faulted.</summary>
    /// <typeparam name="T">The message contract type.</typeparam>
    /// <typeparam name="TException">The exception handled by the member.</typeparam>
    /// <param name="context">The context associated with the operation.</param>
    /// <param name="next">The next pipeline stage to invoke.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="context" /> or <paramref name="next" /> is <see langword="null" />.</exception>
    /// <exception cref="InvalidOperationException">The activity factory returns <see langword="null" />.</exception>
    public Task FaultedAsync<T, TException>(IBehaviorExceptionContext<TSaga, T, TException> context, IBehavior<TSaga, T> next)
        where T : class
        where TException : Exception
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(next);

        IStateMachineActivity<TSaga> activity = GetActivity(context);

        return activity.FaultedAsync(context, next);
    }

    IStateMachineActivity<TSaga> GetActivity(IBehaviorContext<TSaga> context) =>
        _activityFactory(context) ?? throw new InvalidOperationException("The activity factory returned null.");
}


/// <summary>Executes the factory activity.</summary>
/// <typeparam name="TSaga">The saga state managed by the member.</typeparam>
/// <typeparam name="TMessage">The message contract processed by the member.</typeparam>
public class FactoryActivity<TSaga, TMessage> :
    IStateMachineActivity<TSaga, TMessage>
    where TSaga : class, ISagaStateMachineInstance
    where TMessage : class
{
    readonly Func<IBehaviorContext<TSaga, TMessage>, IStateMachineActivity<TSaga, TMessage>> _activityFactory;

    /// <summary>Initializes a new instance.</summary>
    /// <param name="activityFactory">The activity factory.</param>
    /// <exception cref="ArgumentNullException"><paramref name="activityFactory" /> is <see langword="null" />.</exception>
    public FactoryActivity(Func<IBehaviorContext<TSaga, TMessage>, IStateMachineActivity<TSaga, TMessage>> activityFactory)
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

        context.CreateScope("factory");
    }

    /// <summary>Runs the configured action.</summary>
    /// <param name="context">The context associated with the operation.</param>
    /// <param name="next">The next pipeline stage to invoke.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="context" /> or <paramref name="next" /> is <see langword="null" />.</exception>
    /// <exception cref="InvalidOperationException">The activity factory returns <see langword="null" />.</exception>
    public Task ExecuteAsync(IBehaviorContext<TSaga, TMessage> context, IBehavior<TSaga, TMessage> next)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(next);

        IStateMachineActivity<TSaga, TMessage> activity = GetActivity(context);

        return activity.ExecuteAsync(context, next);
    }

    /// <summary>Reports that the operation has faulted.</summary>
    /// <typeparam name="TException">The exception handled by the member.</typeparam>
    /// <param name="context">The context associated with the operation.</param>
    /// <param name="next">The next pipeline stage to invoke.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="context" /> or <paramref name="next" /> is <see langword="null" />.</exception>
    /// <exception cref="InvalidOperationException">The activity factory returns <see langword="null" />.</exception>
    public Task FaultedAsync<TException>(IBehaviorExceptionContext<TSaga, TMessage, TException> context, IBehavior<TSaga, TMessage> next)
        where TException : Exception
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(next);

        IStateMachineActivity<TSaga, TMessage> activity = GetActivity(context);

        return activity.FaultedAsync(context, next);
    }

    IStateMachineActivity<TSaga, TMessage> GetActivity(IBehaviorContext<TSaga, TMessage> context) =>
        _activityFactory(context) ?? throw new InvalidOperationException("The activity factory returned null.");
}
