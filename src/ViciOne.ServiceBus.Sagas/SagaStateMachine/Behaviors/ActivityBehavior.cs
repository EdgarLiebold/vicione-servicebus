using System;
using System.Threading.Tasks;

namespace ViciOne.ServiceBus.SagaStateMachine;

/// <summary>Executes activity state-machine behavior.</summary>
/// <typeparam name="TSaga">The saga state managed by the member.</typeparam>
public class ActivityBehavior<TSaga> :
    IBehavior<TSaga>
    where TSaga : class, SagaStateMachineInstance
{
    readonly IStateMachineActivity<TSaga> _activity;
    readonly IBehavior<TSaga> _next;

    /// <summary>Initializes a new instance.</summary>
    /// <param name="activity">The activity.</param>
    /// <param name="next">The next pipeline stage to invoke.</param>
    public ActivityBehavior(IStateMachineActivity<TSaga> activity, IBehavior<TSaga> next)
    {
        _activity = activity;
        _next = next;
    }

    /// <summary>Accepts the supplied value.</summary>
    /// <param name="visitor">The visitor.</param>
    public void Accept(StateMachineVisitor visitor)
    {
        visitor.Visit(this, x =>
        {
            _activity.Accept(visitor);
            _next.Accept(visitor);
        });
    }

    /// <summary>Writes diagnostic information to the probe context.</summary>
    /// <param name="context">The context associated with the operation.</param>
    public void Probe(ProbeContext context)
    {
        _activity.Probe(context);
        _next.Probe(context);
    }

    /// <summary>Runs the configured action.</summary>
    /// <param name="context">The context associated with the operation.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public async Task ExecuteAsync(BehaviorContext<TSaga> context)
    {
        try
        {
            await _activity.ExecuteAsync(context, _next).ConfigureAwait(false);
        }
        catch (Exception exception)
        {
            await ExceptionTypeCache.FaultedAsync(_next, context, exception).ConfigureAwait(false);
        }
    }

    /// <summary>Runs the configured action.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="context">The context associated with the operation.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public async Task ExecuteAsync<T>(BehaviorContext<TSaga, T> context)
        where T : class
    {
        var behavior = new DataBehavior<TSaga, T>(_next);
        try
        {
            await _activity.ExecuteAsync(context, behavior).ConfigureAwait(false);
        }
        catch (Exception exception)
        {
            await ExceptionTypeCache.FaultedAsync(behavior, context, exception).ConfigureAwait(false);
        }
    }

    /// <summary>Reports that the operation has faulted.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <typeparam name="TException">The exception handled by the member.</typeparam>
    /// <param name="context">The context associated with the operation.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public Task FaultedAsync<T, TException>(BehaviorExceptionContext<TSaga, T, TException> context)
        where T : class
        where TException : Exception
    {
        var behavior = new DataBehavior<TSaga, T>(_next);

        return _activity.FaultedAsync(context, behavior);
    }

    /// <summary>Reports that the operation has faulted.</summary>
    /// <typeparam name="TException">The exception handled by the member.</typeparam>
    /// <param name="context">The context associated with the operation.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public Task FaultedAsync<TException>(BehaviorExceptionContext<TSaga, TException> context)
        where TException : Exception
    {
        return _activity.FaultedAsync(context, _next);
    }
}
