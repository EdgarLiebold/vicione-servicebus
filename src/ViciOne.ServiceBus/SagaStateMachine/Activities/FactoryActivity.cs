using System;
using System.Threading.Tasks;

namespace ViciOne.ServiceBus.SagaStateMachine;

public class FactoryActivity<TSaga> :
    IStateMachineActivity<TSaga>
    where TSaga : class, SagaStateMachineInstance
{
    readonly Func<BehaviorContext<TSaga>, IStateMachineActivity<TSaga>> _activityFactory;

    public FactoryActivity(Func<BehaviorContext<TSaga>, IStateMachineActivity<TSaga>> activityFactory)
    {
        _activityFactory = activityFactory;
    }

    public void Accept(StateMachineVisitor visitor)
    {
        visitor.Visit(this);
    }

    public void Probe(ProbeContext context)
    {
        context.CreateScope("factory");
    }

    public Task ExecuteAsync(BehaviorContext<TSaga> context, IBehavior<TSaga> next)
    {
        IStateMachineActivity<TSaga> activity = _activityFactory(context);

        return activity.ExecuteAsync(context, next);
    }

    public Task ExecuteAsync<T>(BehaviorContext<TSaga, T> context, IBehavior<TSaga, T> next)
        where T : class
    {
        IStateMachineActivity<TSaga> activity = _activityFactory(context);

        return activity.ExecuteAsync(context, next);
    }

    public Task FaultedAsync<TException>(BehaviorExceptionContext<TSaga, TException> context, IBehavior<TSaga> next)
        where TException : Exception
    {
        IStateMachineActivity<TSaga> activity = _activityFactory(context);

        return activity.FaultedAsync(context, next);
    }

    public Task FaultedAsync<T, TException>(BehaviorExceptionContext<TSaga, T, TException> context, IBehavior<TSaga, T> next)
        where T : class
        where TException : Exception
    {
        IStateMachineActivity<TSaga> activity = _activityFactory(context);

        return activity.FaultedAsync(context, next);
    }
}


public class FactoryActivity<TSaga, TMessage> :
    IStateMachineActivity<TSaga, TMessage>
    where TSaga : class, SagaStateMachineInstance
    where TMessage : class
{
    readonly Func<BehaviorContext<TSaga, TMessage>, IStateMachineActivity<TSaga, TMessage>> _activityFactory;

    public FactoryActivity(Func<BehaviorContext<TSaga, TMessage>, IStateMachineActivity<TSaga, TMessage>> activityFactory)
    {
        _activityFactory = activityFactory;
    }

    public void Accept(StateMachineVisitor visitor)
    {
        visitor.Visit(this);
    }

    public void Probe(ProbeContext context)
    {
        context.CreateScope("factory");
    }

    public Task ExecuteAsync(BehaviorContext<TSaga, TMessage> context, IBehavior<TSaga, TMessage> next)
    {
        IStateMachineActivity<TSaga, TMessage> activity = _activityFactory(context);

        return activity.ExecuteAsync(context, next);
    }

    public Task FaultedAsync<TException>(BehaviorExceptionContext<TSaga, TMessage, TException> context, IBehavior<TSaga, TMessage> next)
        where TException : Exception
    {
        IStateMachineActivity<TSaga, TMessage> activity = _activityFactory(context);

        return activity.FaultedAsync(context, next);
    }
}
