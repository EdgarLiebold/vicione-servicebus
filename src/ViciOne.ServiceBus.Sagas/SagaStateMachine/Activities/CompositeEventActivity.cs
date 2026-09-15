using System;
using System.Threading.Tasks;

namespace ViciOne.ServiceBus.SagaStateMachine;

/// <summary>Records a required-event flag and raises the composite event when its complete flag set is reached.</summary>
/// <typeparam name="TSaga">The saga instance type containing the composite-event status.</typeparam>
public class CompositeEventActivity<TSaga> :
    IStateMachineActivity<TSaga>
    where TSaga : class, ISagaStateMachineInstance
{
    readonly ICompositeEventStatusAccessor<TSaga> _accessor;
    readonly CompositeEventStatus _complete;
    readonly int _flag;
    readonly CompositeEventOptions _options;

    /// <summary>Configures the required-event flag, completion status and composite event to raise.</summary>
    /// <param name="accessor">The accessor used to read and write composite status on the saga instance.</param>
    /// <param name="flag">The flag set by this required event.</param>
    /// <param name="complete">The exact status identifying completion of all required events.</param>
    /// <param name="event">The composite event raised when the resulting status equals the complete status.</param>
    /// <param name="options">The options whose RaiseOnce flag suppresses processing of an already-set required-event flag.</param>
    public CompositeEventActivity(ICompositeEventStatusAccessor<TSaga> accessor, int flag, CompositeEventStatus complete, IEvent @event,
        CompositeEventOptions options)
    {
        _accessor = accessor;
        _flag = flag;
        _complete = complete;
        _options = options;
        Event = @event;
    }

    /// <summary>Gets the composite event raised when the tracked status is complete.</summary>
    public IEvent Event { get; }

    /// <summary>Exposes this activity to a state-machine visitor.</summary>
    /// <param name="visitor">The visitor receiving the composite-event activity.</param>
    public void Accept(IStateMachineVisitor visitor)
    {
        visitor.Visit(this);
    }

    /// <summary>Writes diagnostic information to the probe context.</summary>
    /// <param name="context">The parent probe context for the status accessor, composite-event name and required-event flag.</param>
    public void Probe(ProbeContext context)
    {
        var scope = context.CreateScope("compositeEvent");
        _accessor.Probe(scope);
        scope.Add("event", Event.Name);
        scope.Add("flag", _flag.ToString("X8"));
    }

    /// <summary>Updates composite status, awaits any resulting composite event and invokes the remaining behavior.</summary>
    /// <param name="context">The behavior context containing the saga instance.</param>
    /// <param name="next">The remaining behavior invoked after composite processing succeeds.</param>
    /// <returns>A task completing after composite processing and the remaining behavior.</returns>
    public async Task ExecuteAsync(IBehaviorContext<TSaga> context, IBehavior<TSaga> next)
    {
        await ExecuteAsync(context).ConfigureAwait(false);

        await next.ExecuteAsync(context).ConfigureAwait(false);
    }

    /// <summary>Updates composite status for a message event, awaits any resulting composite event and invokes the remaining behavior.</summary>
    /// <typeparam name="TData">The event's message type.</typeparam>
    /// <param name="context">The behavior context containing the saga instance and event message.</param>
    /// <param name="next">The remaining typed behavior invoked after composite processing succeeds.</param>
    /// <returns>A task completing after composite processing and the remaining typed behavior.</returns>
    public async Task ExecuteAsync<TData>(IBehaviorContext<TSaga, TData> context, IBehavior<TSaga, TData> next)
        where TData : class
    {
        await ExecuteAsync(context).ConfigureAwait(false);

        await next.ExecuteAsync(context).ConfigureAwait(false);
    }

    /// <summary>Forwards the fault to the remaining behavior without changing composite status.</summary>
    /// <typeparam name="TException">The fault's exception type.</typeparam>
    /// <param name="context">The faulted saga behavior context.</param>
    /// <param name="next">The remaining fault behavior.</param>
    /// <returns>The remaining behavior's fault-propagation task.</returns>
    public Task FaultedAsync<TException>(IBehaviorExceptionContext<TSaga, TException> context, IBehavior<TSaga> next)
        where TException : Exception
    {
        return next.FaultedAsync(context);
    }

    /// <summary>Forwards the message-event fault to the remaining behavior without changing composite status.</summary>
    /// <typeparam name="T">The event's message type.</typeparam>
    /// <typeparam name="TException">The fault's exception type.</typeparam>
    /// <param name="context">The faulted saga and event-message behavior context.</param>
    /// <param name="next">The remaining typed fault behavior.</param>
    /// <returns>The remaining typed behavior's fault-propagation task.</returns>
    public Task FaultedAsync<T, TException>(IBehaviorExceptionContext<TSaga, T, TException> context, IBehavior<TSaga, T> next)
        where T : class
        where TException : Exception
    {
        return next.FaultedAsync(context);
    }

    Task ExecuteAsync(IBehaviorContext<TSaga> context)
    {
        var value = _accessor.Get(context.Saga);

        if (value.IsSet(_flag) && _options.HasFlag(CompositeEventOptions.RaiseOnce))
            return Task.CompletedTask;

        value.Set(_flag);

        _accessor.Set(context.Saga, value);

        return value.Equals(_complete)
            ? RaiseCompositeEventAsync(context)
            : Task.CompletedTask;
    }

    Task RaiseCompositeEventAsync(IBehaviorContext<TSaga> context)
    {
        return context.RaiseAsync(Event);
    }
}
