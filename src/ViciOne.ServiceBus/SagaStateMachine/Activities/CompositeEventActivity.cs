using System;
using System.Threading.Tasks;

namespace ViciOne.ServiceBus.SagaStateMachine;

public class CompositeEventActivity<TSaga> :
    IStateMachineActivity<TSaga>
    where TSaga : class, SagaStateMachineInstance
{
    readonly ICompositeEventStatusAccessor<TSaga> _accessor;
    readonly CompositeEventStatus _complete;
    readonly int _flag;
    readonly CompositeEventOptions _options;

    public CompositeEventActivity(ICompositeEventStatusAccessor<TSaga> accessor, int flag, CompositeEventStatus complete, Event @event,
        CompositeEventOptions options)
    {
        _accessor = accessor;
        _flag = flag;
        _complete = complete;
        _options = options;
        Event = @event;
    }

    public Event Event { get; }

    public void Accept(StateMachineVisitor visitor)
    {
        visitor.Visit(this);
    }

    public void Probe(ProbeContext context)
    {
        var scope = context.CreateScope("compositeEvent");
        _accessor.Probe(scope);
        scope.Add("event", Event.Name);
        scope.Add("flag", _flag.ToString("X8"));
    }

    public async Task ExecuteAsync(BehaviorContext<TSaga> context, IBehavior<TSaga> next)
    {
        await ExecuteAsync(context).ConfigureAwait(false);

        await next.ExecuteAsync(context).ConfigureAwait(false);
    }

    public async Task ExecuteAsync<TData>(BehaviorContext<TSaga, TData> context, IBehavior<TSaga, TData> next)
        where TData : class
    {
        await ExecuteAsync(context).ConfigureAwait(false);

        await next.ExecuteAsync(context).ConfigureAwait(false);
    }

    public Task FaultedAsync<TException>(BehaviorExceptionContext<TSaga, TException> context, IBehavior<TSaga> next)
        where TException : Exception
    {
        return next.FaultedAsync(context);
    }

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
