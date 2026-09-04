using System;
using System.Threading.Tasks;

namespace ViciOne.ServiceBus.SagaStateMachine;

public class ActivityBehavior<TSaga> :
    IBehavior<TSaga>
    where TSaga : class, SagaStateMachineInstance
{
    readonly IStateMachineActivity<TSaga> _activity;
    readonly IBehavior<TSaga> _next;

    public ActivityBehavior(IStateMachineActivity<TSaga> activity, IBehavior<TSaga> next)
    {
        _activity = activity;
        _next = next;
    }

    public void Accept(StateMachineVisitor visitor)
    {
        visitor.Visit(this, x =>
        {
            _activity.Accept(visitor);
            _next.Accept(visitor);
        });
    }

    public void Probe(ProbeContext context)
    {
        _activity.Probe(context);
        _next.Probe(context);
    }

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

    public Task FaultedAsync<T, TException>(BehaviorExceptionContext<TSaga, T, TException> context)
        where T : class
        where TException : Exception
    {
        var behavior = new DataBehavior<TSaga, T>(_next);

        return _activity.FaultedAsync(context, behavior);
    }

    public Task FaultedAsync<TException>(BehaviorExceptionContext<TSaga, TException> context)
        where TException : Exception
    {
        return _activity.FaultedAsync(context, _next);
    }
}
