using System;
using System.Threading.Tasks;

namespace ViciOne.ServiceBus.SagaStateMachine;

/// <summary>
/// The last behavior either completes the last activity in the behavior or
/// throws the exception if a compensation is in progress
/// </summary>
/// <typeparam name="TSaga"></typeparam>
public class LastBehavior<TSaga> :
    IBehavior<TSaga>
    where TSaga : class, SagaStateMachineInstance
{
    readonly IStateMachineActivity<TSaga> _activity;

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="activity">The activity value.</param>
    public LastBehavior(IStateMachineActivity<TSaga> activity)
    {
        _activity = activity;
    }

    /// <summary>
    /// Performs the accept operation.
    /// </summary>
    /// <param name="visitor">The visitor value.</param>
    public void Accept(StateMachineVisitor visitor)
    {
        _activity.Accept(visitor);
    }

    /// <summary>
    /// Performs the probe operation.
    /// </summary>
    /// <param name="context">The operation context.</param>
    public void Probe(ProbeContext context)
    {
        _activity.Probe(context);
    }

    /// <summary>
    /// Performs the execute operation.
    /// </summary>
    /// <param name="context">The operation context.</param>
    /// <returns>The result of the operation.</returns>
    public Task ExecuteAsync(BehaviorContext<TSaga> context)
    {
        return _activity.ExecuteAsync(context, Behavior.Empty<TSaga>());
    }

    /// <summary>
    /// Performs the execute operation.
    /// </summary>
    /// <typeparam name="T">The t type.</typeparam>
    /// <param name="context">The operation context.</param>
    /// <returns>The result of the operation.</returns>
    public Task ExecuteAsync<T>(BehaviorContext<TSaga, T> context)
        where T : class
    {
        return _activity.ExecuteAsync(context, Behavior.Empty<TSaga, T>());
    }

    /// <summary>
    /// Performs the faulted operation.
    /// </summary>
    /// <typeparam name="T">The t type.</typeparam>
    /// <typeparam name="TException">The t exception type.</typeparam>
    /// <param name="context">The operation context.</param>
    /// <returns>The result of the operation.</returns>
    public Task FaultedAsync<T, TException>(BehaviorExceptionContext<TSaga, T, TException> context)
        where T : class
        where TException : Exception
    {
        return _activity.FaultedAsync(context, Behavior.Faulted<TSaga, T>());
    }

    /// <summary>
    /// Performs the faulted operation.
    /// </summary>
    /// <typeparam name="TException">The t exception type.</typeparam>
    /// <param name="context">The operation context.</param>
    /// <returns>The result of the operation.</returns>
    public Task FaultedAsync<TException>(BehaviorExceptionContext<TSaga, TException> context)
        where TException : Exception
    {
        return _activity.FaultedAsync(context, Behavior.Faulted<TSaga>());
    }
}
