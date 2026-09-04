using System;
using System.Threading.Tasks;

namespace ViciOne.ServiceBus.SagaStateMachine;

/// <summary>
/// Adapts an Activity to a Data Activity context
/// </summary>
/// <typeparam name="TSaga"></typeparam>
/// <typeparam name="TMessage"></typeparam>
public class SlimActivity<TSaga, TMessage> :
    IStateMachineActivity<TSaga, TMessage>
    where TSaga : class, SagaStateMachineInstance
    where TMessage : class
{
    readonly IStateMachineActivity<TSaga> _activity;

    public SlimActivity(IStateMachineActivity<TSaga> activity)
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

    public Task ExecuteAsync(BehaviorContext<TSaga, TMessage> context, IBehavior<TSaga, TMessage> next)
    {
        return _activity.ExecuteAsync(context, next);
    }

    public Task FaultedAsync<TException>(BehaviorExceptionContext<TSaga, TMessage, TException> context, IBehavior<TSaga, TMessage> next)
        where TException : Exception
    {
        return _activity.FaultedAsync(context, next);
    }
}
