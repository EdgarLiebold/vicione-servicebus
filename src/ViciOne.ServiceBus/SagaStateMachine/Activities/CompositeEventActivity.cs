using System;
using System.Threading.Tasks;

namespace ViciOne.ServiceBus.SagaStateMachine;

/// <summary>
/// Provides a composite event activity implementation.
/// </summary>
/// <typeparam name="TSaga">The t saga type.</typeparam>
public class CompositeEventActivity<TSaga> :
    IStateMachineActivity<TSaga>
    where TSaga : class, SagaStateMachineInstance
{
    readonly ICompositeEventStatusAccessor<TSaga> _accessor;
    readonly CompositeEventStatus _complete;
    readonly int _flag;
    readonly CompositeEventOptions _options;

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="accessor">The accessor value.</param>
    /// <param name="flag">The flag value.</param>
    /// <param name="complete">The complete value.</param>
    /// <param name="event">The event value.</param>
    /// <param name="options">The options value.</param>
    public CompositeEventActivity(ICompositeEventStatusAccessor<TSaga> accessor, int flag, CompositeEventStatus complete, Event @event,
        CompositeEventOptions options)
    {
        _accessor = accessor;
        _flag = flag;
        _complete = complete;
        _options = options;
        Event = @event;
    }

    /// <summary>
    /// Gets the event value.
    /// </summary>
    public Event Event { get; }

    /// <summary>
    /// Performs the accept operation.
    /// </summary>
    /// <param name="visitor">The visitor value.</param>
    public void Accept(StateMachineVisitor visitor)
    {
        visitor.Visit(this);
    }

    /// <summary>
    /// Performs the probe operation.
    /// </summary>
    /// <param name="context">The operation context.</param>
    public void Probe(ProbeContext context)
    {
        var scope = context.CreateScope("compositeEvent");
        _accessor.Probe(scope);
        scope.Add("event", Event.Name);
        scope.Add("flag", _flag.ToString("X8"));
    }

    /// <summary>
    /// Performs the execute operation.
    /// </summary>
    /// <param name="context">The operation context.</param>
    /// <param name="next">The next value.</param>
    /// <returns>The result of the operation.</returns>
    public async Task ExecuteAsync(BehaviorContext<TSaga> context, IBehavior<TSaga> next)
    {
        await ExecuteAsync(context).ConfigureAwait(false);

        await next.ExecuteAsync(context).ConfigureAwait(false);
    }

    /// <summary>
    /// Performs the execute operation.
    /// </summary>
    /// <typeparam name="TData">The t data type.</typeparam>
    /// <param name="context">The operation context.</param>
    /// <param name="next">The next value.</param>
    /// <returns>The result of the operation.</returns>
    public async Task ExecuteAsync<TData>(BehaviorContext<TSaga, TData> context, IBehavior<TSaga, TData> next)
        where TData : class
    {
        await ExecuteAsync(context).ConfigureAwait(false);

        await next.ExecuteAsync(context).ConfigureAwait(false);
    }

    /// <summary>
    /// Performs the faulted operation.
    /// </summary>
    /// <typeparam name="TException">The t exception type.</typeparam>
    /// <param name="context">The operation context.</param>
    /// <param name="next">The next value.</param>
    /// <returns>The result of the operation.</returns>
    public Task FaultedAsync<TException>(BehaviorExceptionContext<TSaga, TException> context, IBehavior<TSaga> next)
        where TException : Exception
    {
        return next.FaultedAsync(context);
    }

    /// <summary>
    /// Performs the faulted operation.
    /// </summary>
    /// <typeparam name="T">The t type.</typeparam>
    /// <typeparam name="TException">The t exception type.</typeparam>
    /// <param name="context">The operation context.</param>
    /// <param name="next">The next value.</param>
    /// <returns>The result of the operation.</returns>
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
