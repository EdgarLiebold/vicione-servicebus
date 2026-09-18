using System;
using System.Threading.Tasks;

namespace ViciOne.ServiceBus.SagaStateMachine;

/// <summary>
/// The last behavior either completes the last activity in the behavior or
/// throws the exception if a compensation is in progress.
/// </summary>
/// <typeparam name="TSaga">The saga state managed by the member.</typeparam>
public class LastBehavior<TSaga> :
    IBehavior<TSaga>
    where TSaga : class, ISagaStateMachineInstance
{
    readonly IStateMachineActivity<TSaga> _activity;

    /// <summary>Initializes a new instance.</summary>
    /// <param name="activity">The activity.</param>
    public LastBehavior(IStateMachineActivity<TSaga> activity)
    {
        _activity = activity ?? throw new ArgumentNullException(nameof(activity));
    }

    /// <summary>Accepts the supplied value.</summary>
    /// <param name="visitor">The visitor.</param>
    public void Accept(IStateMachineVisitor visitor)
    {
        ArgumentNullException.ThrowIfNull(visitor);
        _activity.Accept(visitor);
    }

    /// <summary>Writes diagnostic information to the probe context.</summary>
    /// <param name="context">The context associated with the operation.</param>
    public void Probe(ProbeContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        _activity.Probe(context);
    }

    /// <summary>Runs the configured action.</summary>
    /// <param name="context">The context associated with the operation.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public Task ExecuteAsync(IBehaviorContext<TSaga> context)
    {
        ArgumentNullException.ThrowIfNull(context);
        return _activity.ExecuteAsync(context, Behavior.Empty<TSaga>());
    }

    /// <summary>Runs the configured action.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="context">The context associated with the operation.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public Task ExecuteAsync<T>(IBehaviorContext<TSaga, T> context)
        where T : class
    {
        ArgumentNullException.ThrowIfNull(context);
        return _activity.ExecuteAsync(context, Behavior.Empty<TSaga, T>());
    }

    /// <summary>Reports that the operation has faulted.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <typeparam name="TException">The exception handled by the member.</typeparam>
    /// <param name="context">The context associated with the operation.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public Task FaultedAsync<T, TException>(IBehaviorExceptionContext<TSaga, T, TException> context)
        where T : class
        where TException : Exception
    {
        ArgumentNullException.ThrowIfNull(context);
        return _activity.FaultedAsync(context, Behavior.Faulted<TSaga, T>());
    }

    /// <summary>Reports that the operation has faulted.</summary>
    /// <typeparam name="TException">The exception handled by the member.</typeparam>
    /// <param name="context">The context associated with the operation.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public Task FaultedAsync<TException>(IBehaviorExceptionContext<TSaga, TException> context)
        where TException : Exception
    {
        ArgumentNullException.ThrowIfNull(context);
        return _activity.FaultedAsync(context, Behavior.Faulted<TSaga>());
    }
}
