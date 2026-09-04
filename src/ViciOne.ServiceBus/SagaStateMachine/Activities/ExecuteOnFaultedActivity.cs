using System;
using System.Threading.Tasks;

namespace ViciOne.ServiceBus.SagaStateMachine;

public class ExecuteOnFaultedActivity<TSaga> :
    IStateMachineActivity<TSaga>
    where TSaga : class, SagaStateMachineInstance
{
    readonly IStateMachineActivity<TSaga> _activity;

    public ExecuteOnFaultedActivity(IStateMachineActivity<TSaga> activity)
    {
        _activity = activity;
    }

    public void Accept(StateMachineVisitor visitor)
    {
        _activity.Accept(visitor);
    }

    public void Probe(ProbeContext context)
    {
        _activity.Probe(context);
    }

    public Task ExecuteAsync(BehaviorContext<TSaga> context, IBehavior<TSaga> next)
    {
        return next.ExecuteAsync(context);
    }

    public Task ExecuteAsync<T>(BehaviorContext<TSaga, T> context, IBehavior<TSaga, T> next)
        where T : class
    {
        return next.ExecuteAsync(context);
    }

    public Task FaultedAsync<TException>(BehaviorExceptionContext<TSaga, TException> context, IBehavior<TSaga> next)
        where TException : Exception
    {
        var nextBehavior = new ExecuteOnFaultedBehavior<TSaga, TException>(next, context);

        return _activity.ExecuteAsync(context, nextBehavior);
    }

    public Task FaultedAsync<T, TException>(BehaviorExceptionContext<TSaga, T, TException> context, IBehavior<TSaga, T> next)
        where TException : Exception
        where T : class
    {
        var nextBehavior = new ExecuteOnFaultedBehavior<TSaga, T, TException>(next, context);

        return _activity.ExecuteAsync(context, nextBehavior);
    }
}
