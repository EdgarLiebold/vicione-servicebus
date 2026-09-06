using System;
using System.Threading.Tasks;

namespace ViciOne.ServiceBus.SagaStateMachine;

/// <summary>Executes the execute on faulted activity.</summary>
/// <typeparam name="TSaga">The saga state managed by the member.</typeparam>
public class ExecuteOnFaultedActivity<TSaga> :
    IStateMachineActivity<TSaga>
    where TSaga : class, SagaStateMachineInstance
{
    readonly IStateMachineActivity<TSaga> _activity;

    /// <summary>Initializes a new instance.</summary>
    /// <param name="activity">The activity.</param>
    public ExecuteOnFaultedActivity(IStateMachineActivity<TSaga> activity)
    {
        _activity = activity;
    }

    /// <summary>Accepts the supplied value.</summary>
    /// <param name="visitor">The visitor.</param>
    public void Accept(StateMachineVisitor visitor)
    {
        _activity.Accept(visitor);
    }

    /// <summary>Writes diagnostic information to the probe context.</summary>
    /// <param name="context">The context associated with the operation.</param>
    public void Probe(ProbeContext context)
    {
        _activity.Probe(context);
    }

    /// <summary>Runs the configured action.</summary>
    /// <param name="context">The context associated with the operation.</param>
    /// <param name="next">The next pipeline stage to invoke.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public Task ExecuteAsync(BehaviorContext<TSaga> context, IBehavior<TSaga> next)
    {
        return next.ExecuteAsync(context);
    }

    /// <summary>Runs the configured action.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="context">The context associated with the operation.</param>
    /// <param name="next">The next pipeline stage to invoke.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public Task ExecuteAsync<T>(BehaviorContext<TSaga, T> context, IBehavior<TSaga, T> next)
        where T : class
    {
        return next.ExecuteAsync(context);
    }

    /// <summary>Reports that the operation has faulted.</summary>
    /// <typeparam name="TException">The exception handled by the member.</typeparam>
    /// <param name="context">The context associated with the operation.</param>
    /// <param name="next">The next pipeline stage to invoke.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public Task FaultedAsync<TException>(BehaviorExceptionContext<TSaga, TException> context, IBehavior<TSaga> next)
        where TException : Exception
    {
        var nextBehavior = new ExecuteOnFaultedBehavior<TSaga, TException>(next, context);

        return _activity.ExecuteAsync(context, nextBehavior);
    }

    /// <summary>Reports that the operation has faulted.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <typeparam name="TException">The exception handled by the member.</typeparam>
    /// <param name="context">The context associated with the operation.</param>
    /// <param name="next">The next pipeline stage to invoke.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public Task FaultedAsync<T, TException>(BehaviorExceptionContext<TSaga, T, TException> context, IBehavior<TSaga, T> next)
        where TException : Exception
        where T : class
    {
        var nextBehavior = new ExecuteOnFaultedBehavior<TSaga, T, TException>(next, context);

        return _activity.ExecuteAsync(context, nextBehavior);
    }
}
