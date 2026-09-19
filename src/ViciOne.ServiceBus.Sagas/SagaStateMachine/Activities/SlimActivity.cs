using System;
using System.Threading.Tasks;

namespace ViciOne.ServiceBus.SagaStateMachine;

/// <summary>Adapts an Activity to a Data Activity context.</summary>
/// <typeparam name="TSaga">The saga state managed by the member.</typeparam>
/// <typeparam name="TMessage">The message contract processed by the member.</typeparam>
public class SlimActivity<TSaga, TMessage> :
    IStateMachineActivity<TSaga, TMessage>
    where TSaga : class, ISagaStateMachineInstance
    where TMessage : class
{
    readonly IStateMachineActivity<TSaga> _activity;

    /// <summary>Initializes a new instance.</summary>
    /// <param name="activity">The activity.</param>
    /// <exception cref="ArgumentNullException"><paramref name="activity" /> is <see langword="null" />.</exception>
    public SlimActivity(IStateMachineActivity<TSaga> activity)
    {
        _activity = activity ?? throw new ArgumentNullException(nameof(activity));
    }

    /// <summary>Accepts the supplied value.</summary>
    /// <param name="visitor">The visitor.</param>
    /// <exception cref="ArgumentNullException"><paramref name="visitor" /> is <see langword="null" />.</exception>
    public void Accept(IStateMachineVisitor visitor)
    {
        ArgumentNullException.ThrowIfNull(visitor);

        _activity.Accept(visitor);
    }

    /// <summary>Writes diagnostic information to the probe context.</summary>
    /// <param name="context">The context associated with the operation.</param>
    /// <exception cref="ArgumentNullException"><paramref name="context" /> is <see langword="null" />.</exception>
    public void Probe(ProbeContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        _activity.Probe(context);
    }

    /// <summary>Runs the configured action.</summary>
    /// <param name="context">The context associated with the operation.</param>
    /// <param name="next">The next pipeline stage to invoke.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="context" /> or <paramref name="next" /> is <see langword="null" />.</exception>
    public Task ExecuteAsync(IBehaviorContext<TSaga, TMessage> context, IBehavior<TSaga, TMessage> next)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(next);

        return _activity.ExecuteAsync(context, next);
    }

    /// <summary>Reports that the operation has faulted.</summary>
    /// <typeparam name="TException">The exception handled by the member.</typeparam>
    /// <param name="context">The context associated with the operation.</param>
    /// <param name="next">The next pipeline stage to invoke.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="context" /> or <paramref name="next" /> is <see langword="null" />.</exception>
    public Task FaultedAsync<TException>(IBehaviorExceptionContext<TSaga, TMessage, TException> context, IBehavior<TSaga, TMessage> next)
        where TException : Exception
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(next);

        return _activity.FaultedAsync(context, next);
    }
}
