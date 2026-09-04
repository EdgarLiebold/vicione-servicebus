using System;
using System.Threading.Tasks;

namespace ViciOne.ServiceBus.SagaStateMachine;

/// <summary>
/// Provides an activity behavior implementation.
/// </summary>
/// <typeparam name="TSaga">The t saga type.</typeparam>
public class ActivityBehavior<TSaga> :
    IBehavior<TSaga>
    where TSaga : class, SagaStateMachineInstance
{
    readonly IStateMachineActivity<TSaga> _activity;
    readonly IBehavior<TSaga> _next;

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="activity">The activity value.</param>
    /// <param name="next">The next value.</param>
    public ActivityBehavior(IStateMachineActivity<TSaga> activity, IBehavior<TSaga> next)
    {
        _activity = activity;
        _next = next;
    }

    /// <summary>
    /// Performs the accept operation.
    /// </summary>
    /// <param name="visitor">The visitor value.</param>
    public void Accept(StateMachineVisitor visitor)
    {
        visitor.Visit(this, x =>
        {
            _activity.Accept(visitor);
            _next.Accept(visitor);
        });
    }

    /// <summary>
    /// Performs the probe operation.
    /// </summary>
    /// <param name="context">The operation context.</param>
    public void Probe(ProbeContext context)
    {
        _activity.Probe(context);
        _next.Probe(context);
    }

    /// <summary>
    /// Performs the execute operation.
    /// </summary>
    /// <param name="context">The operation context.</param>
    /// <returns>The result of the operation.</returns>
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

    /// <summary>
    /// Performs the execute operation.
    /// </summary>
    /// <typeparam name="T">The t type.</typeparam>
    /// <param name="context">The operation context.</param>
    /// <returns>The result of the operation.</returns>
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
        var behavior = new DataBehavior<TSaga, T>(_next);

        return _activity.FaultedAsync(context, behavior);
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
        return _activity.FaultedAsync(context, _next);
    }
}
