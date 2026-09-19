using System;
using System.Threading.Tasks;

namespace ViciOne.ServiceBus.SagaStateMachine;

/// <summary>Executes the execute on faulted activity.</summary>
/// <typeparam name="TSaga">The saga state managed by the member.</typeparam>
public class ExecuteOnFaultedActivity<TSaga> :
    IStateMachineActivity<TSaga>
    where TSaga : class, ISagaStateMachineInstance
{
    readonly IStateMachineActivity<TSaga> _activity;

    /// <summary>Initializes a new instance.</summary>
    /// <param name="activity">The activity.</param>
    /// <exception cref="ArgumentNullException"><paramref name="activity"/> is null.</exception>
    public ExecuteOnFaultedActivity(IStateMachineActivity<TSaga> activity)
    {
        _activity = activity ?? throw new ArgumentNullException(nameof(activity));
    }

    /// <summary>Accepts the supplied value.</summary>
    /// <param name="visitor">The visitor.</param>
    /// <exception cref="ArgumentNullException"><paramref name="visitor"/> is null.</exception>
    public void Accept(IStateMachineVisitor visitor)
    {
        ArgumentNullException.ThrowIfNull(visitor);
        _activity.Accept(visitor);
    }

    /// <summary>Writes diagnostic information to the probe context.</summary>
    /// <param name="context">The context associated with the operation.</param>
    /// <exception cref="ArgumentNullException"><paramref name="context"/> is null.</exception>
    public void Probe(ProbeContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        _activity.Probe(context);
    }

    /// <summary>Runs the configured action.</summary>
    /// <param name="context">The context associated with the operation.</param>
    /// <param name="next">The next pipeline stage to invoke.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="context"/> or <paramref name="next"/> is null.</exception>
    public Task ExecuteAsync(IBehaviorContext<TSaga> context, IBehavior<TSaga> next)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(next);
        return next.ExecuteAsync(context);
    }

    /// <summary>Runs the configured action.</summary>
    /// <typeparam name="T">The message contract type.</typeparam>
    /// <param name="context">The context associated with the operation.</param>
    /// <param name="next">The next pipeline stage to invoke.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="context"/> or <paramref name="next"/> is null.</exception>
    public Task ExecuteAsync<T>(IBehaviorContext<TSaga, T> context, IBehavior<TSaga, T> next)
        where T : class
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(next);
        return next.ExecuteAsync(context);
    }

    /// <summary>Reports that the operation has faulted.</summary>
    /// <typeparam name="TException">The exception handled by the member.</typeparam>
    /// <param name="context">The context associated with the operation.</param>
    /// <param name="next">The next pipeline stage to invoke.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="context"/> or <paramref name="next"/> is null.</exception>
    public Task FaultedAsync<TException>(IBehaviorExceptionContext<TSaga, TException> context, IBehavior<TSaga> next)
        where TException : Exception
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(next);
        var nextBehavior = new ExecuteOnFaultedBehavior<TSaga, TException>(next, context);

        return _activity.ExecuteAsync(context, nextBehavior);
    }

    /// <summary>Reports that the operation has faulted.</summary>
    /// <typeparam name="T">The message contract type.</typeparam>
    /// <typeparam name="TException">The exception handled by the member.</typeparam>
    /// <param name="context">The context associated with the operation.</param>
    /// <param name="next">The next pipeline stage to invoke.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="context"/> or <paramref name="next"/> is null.</exception>
    public Task FaultedAsync<T, TException>(IBehaviorExceptionContext<TSaga, T, TException> context, IBehavior<TSaga, T> next)
        where TException : Exception
        where T : class
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(next);
        var nextBehavior = new ExecuteOnFaultedBehavior<TSaga, T, TException>(next, context);

        return _activity.ExecuteAsync(context, nextBehavior);
    }
}
