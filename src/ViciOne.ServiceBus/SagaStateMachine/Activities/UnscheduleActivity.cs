using System;
using System.Threading.Tasks;

namespace ViciOne.ServiceBus.SagaStateMachine;

public class UnscheduleActivity<TSaga> :
    IStateMachineActivity<TSaga>
    where TSaga : class, SagaStateMachineInstance
{
    readonly Schedule<TSaga> _schedule;

    public UnscheduleActivity(Schedule<TSaga> schedule)
    {
        _schedule = schedule ?? throw new ArgumentNullException(nameof(schedule));
    }

    public void Accept(StateMachineVisitor inspector)
    {
        inspector.Visit(this);
    }

    public void Probe(ProbeContext context)
    {
        context.CreateScope("unschedule");
    }

    public async Task ExecuteAsync(BehaviorContext<TSaga> context, IBehavior<TSaga> next)
    {
        await ExecuteAsync(context).ConfigureAwait(false);

        await next.ExecuteAsync(context).ConfigureAwait(false);
    }

    public async Task ExecuteAsync<T>(BehaviorContext<TSaga, T> context, IBehavior<TSaga, T> next)
        where T : class
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

    async Task ExecuteAsync(SagaConsumeContext<TSaga> context)
    {
        Guid? previousTokenId = _schedule.GetTokenId(context.Saga);
        if (previousTokenId.HasValue)
        {
            Guid? messageTokenId = context.GetSchedulingTokenId();
            if (!messageTokenId.HasValue || previousTokenId.Value != messageTokenId.Value)
            {
                var schedulerContext = context.GetPayload<MessageSchedulerContext>();

                await schedulerContext.CancelScheduledSendAsync(context.ReceiveContext.InputAddress, previousTokenId.Value, context.CancellationToken)
                    .ConfigureAwait(false);

                _schedule.SetTokenId(context.Saga, null);
            }
        }
    }
}
