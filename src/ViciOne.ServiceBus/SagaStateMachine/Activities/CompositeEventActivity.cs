using System;
using System.Threading.Tasks;

namespace ViciOne.ServiceBus.SagaStateMachine;

/// <summary>Executes the composite event activity.</summary>
/// <typeparam name="TSaga">The saga state managed by the member.</typeparam>
public class CompositeEventActivity<TSaga> :
    IStateMachineActivity<TSaga>
    where TSaga : class, SagaStateMachineInstance
{
    readonly ICompositeEventStatusAccessor<TSaga> _accessor;
    readonly CompositeEventStatus _complete;
    readonly int _flag;
    readonly CompositeEventOptions _options;

    /// <summary>Initializes a new instance.</summary>
    /// <param name="accessor">The accessor.</param>
    /// <param name="flag">The flag.</param>
    /// <param name="complete">The complete.</param>
    /// <param name="event">The event.</param>
    /// <param name="options">The options that control the operation.</param>
    public CompositeEventActivity(ICompositeEventStatusAccessor<TSaga> accessor, int flag, CompositeEventStatus complete, Event @event,
        CompositeEventOptions options)
    {
        _accessor = accessor;
        _flag = flag;
        _complete = complete;
        _options = options;
        Event = @event;
    }

    /// <summary>Gets the event.</summary>
    public Event Event { get; }

    /// <summary>Accepts the supplied value.</summary>
    /// <param name="visitor">The visitor.</param>
    public void Accept(StateMachineVisitor visitor)
    {
        visitor.Visit(this);
    }

    /// <summary>Writes diagnostic information to the probe context.</summary>
    /// <param name="context">The context associated with the operation.</param>
    public void Probe(ProbeContext context)
    {
        var scope = context.CreateScope("compositeEvent");
        _accessor.Probe(scope);
        scope.Add("event", Event.Name);
        scope.Add("flag", _flag.ToString("X8"));
    }

    /// <summary>Runs the configured action.</summary>
    /// <param name="context">The context associated with the operation.</param>
    /// <param name="next">The next pipeline stage to invoke.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public async Task ExecuteAsync(BehaviorContext<TSaga> context, IBehavior<TSaga> next)
    {
        await ExecuteAsync(context).ConfigureAwait(false);

        await next.ExecuteAsync(context).ConfigureAwait(false);
    }

    /// <summary>Runs the configured action.</summary>
    /// <typeparam name="TData">The data type.</typeparam>
    /// <param name="context">The context associated with the operation.</param>
    /// <param name="next">The next pipeline stage to invoke.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public async Task ExecuteAsync<TData>(BehaviorContext<TSaga, TData> context, IBehavior<TSaga, TData> next)
        where TData : class
    {
        await ExecuteAsync(context).ConfigureAwait(false);

        await next.ExecuteAsync(context).ConfigureAwait(false);
    }

    /// <summary>Reports that the operation has faulted.</summary>
    /// <typeparam name="TException">The exception handled by the member.</typeparam>
    /// <param name="context">The context associated with the operation.</param>
    /// <param name="next">The next pipeline stage to invoke.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public Task FaultedAsync<TException>(BehaviorExceptionContext<TSaga, TException> context, IBehavior<TSaga> next)
        where TException : Exception
    {
        return next.FaultedAsync(context);
    }

    /// <summary>Reports that the operation has faulted.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <typeparam name="TException">The exception handled by the member.</typeparam>
    /// <param name="context">The context associated with the operation.</param>
    /// <param name="next">The next pipeline stage to invoke.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public Task FaultedAsync<T, TException>(BehaviorExceptionContext<TSaga, T, TException> context, IBehavior<TSaga, T> next)
        where T : class
        where TException : Exception
    {
        return next.FaultedAsync(context);
    }

    Task ExecuteAsync(BehaviorContext<TSaga> context)
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

    Task RaiseCompositeEventAsync(BehaviorContext<TSaga> context)
    {
        return context.RaiseAsync(Event);
    }
}
