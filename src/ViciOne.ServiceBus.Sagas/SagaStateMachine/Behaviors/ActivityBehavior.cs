using System;
using System.Threading.Tasks;

namespace ViciOne.ServiceBus.SagaStateMachine;

/// <summary>Composes one state-machine activity with the remaining behavior and its fault path.</summary>
/// <typeparam name="TSaga">The saga state type.</typeparam>
internal sealed class ActivityBehavior<TSaga> :
    IBehavior<TSaga>
    where TSaga : class, ISagaStateMachineInstance
{
    readonly IStateMachineActivity<TSaga> _activity;
    readonly IBehavior<TSaga> _next;

    /// <summary>Creates a behavior node from an activity and its continuation.</summary>
    /// <param name="activity">The activity executed by this node.</param>
    /// <param name="next">The behavior invoked after the activity.</param>
    public ActivityBehavior(IStateMachineActivity<TSaga> activity, IBehavior<TSaga> next)
    {
        _activity = activity ?? throw new ArgumentNullException(nameof(activity));
        _next = next ?? throw new ArgumentNullException(nameof(next));
    }

    /// <summary>Visits this behavior and its complete activity chain.</summary>
    /// <param name="visitor">The state-machine visitor.</param>
    public void Accept(IStateMachineVisitor visitor)
    {
        ArgumentNullException.ThrowIfNull(visitor);
        visitor.Visit(this, x =>
        {
            _activity.Accept(visitor);
            _next.Accept(visitor);
        });
    }

    /// <summary>Writes diagnostics for this activity and its continuation.</summary>
    /// <param name="context">The diagnostic context to populate.</param>
    public void Probe(ProbeContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        _activity.Probe(context);
        _next.Probe(context);
    }

    /// <summary>Executes the activity chain and routes non-cancellation failures through its fault chain.</summary>
    /// <param name="context">The saga behavior context.</param>
    /// <returns>A task that completes after execution or fault handling.</returns>
    public async Task ExecuteAsync(IBehaviorContext<TSaga> context)
    {
        ArgumentNullException.ThrowIfNull(context);
        context.CancellationToken.ThrowIfCancellationRequested();

        try
        {
            await _activity.ExecuteAsync(context, _next).ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (context.CancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception)
        {
            await ExceptionTypeCache.FaultedAsync(_next, context, exception).ConfigureAwait(false);
        }
    }

    /// <summary>Executes a data-event activity chain and routes non-cancellation failures through its fault chain.</summary>
    /// <typeparam name="T">The event data type.</typeparam>
    /// <param name="context">The saga and event data.</param>
    /// <returns>A task that completes after execution or fault handling.</returns>
    public async Task ExecuteAsync<T>(IBehaviorContext<TSaga, T> context)
        where T : class
    {
        ArgumentNullException.ThrowIfNull(context);
        context.CancellationToken.ThrowIfCancellationRequested();

        var behavior = new DataBehavior<TSaga, T>(_next);
        try
        {
            await _activity.ExecuteAsync(context, behavior).ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (context.CancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception)
        {
            await ExceptionTypeCache.FaultedAsync(behavior, context, exception).ConfigureAwait(false);
        }
    }

    /// <summary>Runs this activity's typed data-event fault behavior.</summary>
    /// <typeparam name="T">The event data type.</typeparam>
    /// <typeparam name="TException">The exception type.</typeparam>
    /// <param name="context">The faulted saga and event data.</param>
    /// <returns>A task that completes after fault handling.</returns>
    public Task FaultedAsync<T, TException>(IBehaviorExceptionContext<TSaga, T, TException> context)
        where T : class
        where TException : Exception
    {
        ArgumentNullException.ThrowIfNull(context);
        var behavior = new DataBehavior<TSaga, T>(_next);

        return _activity.FaultedAsync(context, behavior);
    }

    /// <summary>Runs this activity's fault behavior.</summary>
    /// <typeparam name="TException">The exception type.</typeparam>
    /// <param name="context">The faulted saga behavior context.</param>
    /// <returns>A task that completes after fault handling.</returns>
    public Task FaultedAsync<TException>(IBehaviorExceptionContext<TSaga, TException> context)
        where TException : Exception
    {
        ArgumentNullException.ThrowIfNull(context);
        return _activity.FaultedAsync(context, _next);
    }
}
