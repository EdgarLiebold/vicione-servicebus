using System;
using System.Threading.Tasks;

namespace ViciOne.ServiceBus.SagaStateMachine;

public class FaultedUnscheduleActivity<TSaga> :
    IStateMachineActivity<TSaga>
    where TSaga : class, SagaStateMachineInstance
{
    readonly Schedule<TSaga> _schedule;

    public FaultedUnscheduleActivity(Schedule<TSaga> schedule)
    {
        _schedule = schedule;
    }

    public void Accept(StateMachineVisitor inspector)
    {
        inspector.Visit(this);
    }

    public void Probe(ProbeContext context)
    {
        context.CreateScope("unschedule-faulted");
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

    public async Task FaultedAsync<TException>(BehaviorExceptionContext<TSaga, TException> context, IBehavior<TSaga> next)
        where TException : Exception
    {
        await FaultedAsync(context).ConfigureAwait(false);

        await next.FaultedAsync(context).ConfigureAwait(false);
    }

    public async Task FaultedAsync<T, TException>(BehaviorExceptionContext<TSaga, T, TException> context, IBehavior<TSaga, T> next)
        where T : class
        where TException : Exception
    {
        await FaultedAsync(context).ConfigureAwait(false);

        await next.FaultedAsync(context).ConfigureAwait(false);
    }

    async Task FaultedAsync(SagaConsumeContext<TSaga> context)
    {
        var schedulerContext = context.GetPayload<MessageSchedulerContext>();

        Guid? previousTokenId = _schedule.GetTokenId(context.Saga);
        if (previousTokenId.HasValue)
        {
            await schedulerContext.CancelScheduledSendAsync(context.ReceiveContext.InputAddress, previousTokenId.Value, context.CancellationToken)
                .ConfigureAwait(false);

            _schedule.SetTokenId(context.Saga, null);
        }
    }
}
