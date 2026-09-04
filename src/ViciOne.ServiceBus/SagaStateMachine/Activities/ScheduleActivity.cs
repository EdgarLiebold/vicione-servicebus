using System;
using System.Threading.Tasks;

namespace ViciOne.ServiceBus.SagaStateMachine;

public class ScheduleActivity<TSaga, TMessage> :
    IStateMachineActivity<TSaga>
    where TSaga : class, SagaStateMachineInstance
    where TMessage : class
{
    readonly ContextMessageFactory<BehaviorContext<TSaga>, TMessage> _messageFactory;
    readonly Schedule<TSaga> _schedule = null!;
    readonly ScheduleTimeProvider<TSaga> _timeProvider;

    public ScheduleActivity(Schedule<TSaga> schedule,
        ScheduleTimeProvider<TSaga> timeProvider, ContextMessageFactory<BehaviorContext<TSaga>, TMessage> messageFactory)
    {
        _messageFactory = messageFactory;
        _schedule = schedule;
        _timeProvider = timeProvider;
    }

    public void Accept(StateMachineVisitor inspector)
    {
        inspector.Visit(this);
    }

    public void Probe(ProbeContext context)
    {
        context.CreateScope("schedule");
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

    async Task ExecuteAsync(BehaviorContext<TSaga> context)
    {
        Guid? previousTokenId = _schedule.GetTokenId(context.Saga);

        var schedulerContext = context.GetPayload<MessageSchedulerContext>();

        ScheduledMessage<TMessage> message = await _messageFactory
            .UseAsync(context, (ctx, s) => schedulerContext.ScheduleSendAsync(_timeProvider(ctx), s.Message, s.Pipe, ctx.CancellationToken)).ConfigureAwait(false);

        _schedule?.SetTokenId(context.Saga, message.TokenId);

        if (previousTokenId.HasValue)
        {
            Guid? messageTokenId = context.GetSchedulingTokenId();
            if (!messageTokenId.HasValue || previousTokenId.Value != messageTokenId.Value)
            {
                await schedulerContext.CancelScheduledSendAsync(context.ReceiveContext.InputAddress, previousTokenId.Value, context.CancellationToken)
                    .ConfigureAwait(false);
            }
        }
    }
}


public class ScheduleActivity<TSaga, TMessage, T> :
    IStateMachineActivity<TSaga, TMessage>
    where TSaga : class, SagaStateMachineInstance
    where TMessage : class
    where T : class
{
    readonly ContextMessageFactory<BehaviorContext<TSaga, TMessage>, T> _messageFactory;
    readonly Schedule<TSaga, T> _schedule;
    readonly ScheduleTimeProvider<TSaga, TMessage> _timeProvider;

    public ScheduleActivity(Schedule<TSaga, T> schedule,
        ScheduleTimeProvider<TSaga, TMessage> timeProvider, ContextMessageFactory<BehaviorContext<TSaga, TMessage>, T> messageFactory)
    {
        _messageFactory = messageFactory;
        _schedule = schedule ?? throw new ArgumentNullException(nameof(schedule));
        _timeProvider = timeProvider;
    }

    public void Accept(StateMachineVisitor inspector)
    {
        inspector.Visit(this);
    }

    public void Probe(ProbeContext context)
    {
        context.CreateScope("schedule");
    }

    public async Task ExecuteAsync(BehaviorContext<TSaga, TMessage> context, IBehavior<TSaga, TMessage> next)
    {
        Guid? previousTokenId = _schedule.GetTokenId(context.Saga);

        var schedulerContext = context.GetPayload<MessageSchedulerContext>();

        ScheduledMessage<T> message = await _messageFactory
            .UseAsync(context, (ctx, s) => schedulerContext.ScheduleSendAsync(_timeProvider(ctx), s.Message, s.Pipe, ctx.CancellationToken)).ConfigureAwait(false);

        _schedule.SetTokenId(context.Saga, message.TokenId);

        if (previousTokenId.HasValue)
        {
            Guid? messageTokenId = context.GetSchedulingTokenId();
            if (!messageTokenId.HasValue || previousTokenId.Value != messageTokenId.Value)
            {
                await schedulerContext.CancelScheduledSendAsync(context.ReceiveContext.InputAddress, previousTokenId.Value, context.CancellationToken)
                    .ConfigureAwait(false);
            }
        }

        await next.ExecuteAsync(context).ConfigureAwait(false);
    }

    public Task FaultedAsync<TException>(BehaviorExceptionContext<TSaga, TMessage, TException> context, IBehavior<TSaga, TMessage> next)
        where TException : Exception
    {
        return next.FaultedAsync(context);
    }
}
