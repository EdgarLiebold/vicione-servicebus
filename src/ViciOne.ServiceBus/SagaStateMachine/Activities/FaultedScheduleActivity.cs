using System;
using System.Threading.Tasks;

namespace ViciOne.ServiceBus.SagaStateMachine;

/// <summary>
/// Provides a faulted schedule activity implementation.
/// </summary>
/// <typeparam name="TSaga">The t saga type.</typeparam>
/// <typeparam name="TException">The t exception type.</typeparam>
/// <typeparam name="TMessage">The t message type.</typeparam>
public class FaultedScheduleActivity<TSaga, TException, TMessage> :
    IStateMachineActivity<TSaga>
    where TSaga : class, SagaStateMachineInstance
    where TException : Exception
    where TMessage : class
{
    readonly ContextMessageFactory<BehaviorExceptionContext<TSaga, TException>, TMessage> _messageFactory;
    readonly Schedule<TSaga, TMessage> _schedule;
    readonly ScheduleTimeExceptionProvider<TSaga, TException> _timeProvider;

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="schedule">The schedule value.</param>
    /// <param name="timeProvider">The time provider value.</param>
    /// <param name="messageFactory">The message factory value.</param>
    public FaultedScheduleActivity(Schedule<TSaga, TMessage> schedule, ScheduleTimeExceptionProvider<TSaga, TException> timeProvider,
        ContextMessageFactory<BehaviorExceptionContext<TSaga, TException>, TMessage> messageFactory)
    {
        _messageFactory = messageFactory;
        _schedule = schedule;
        _timeProvider = timeProvider;
    }

    /// <summary>
    /// Performs the accept operation.
    /// </summary>
    /// <param name="inspector">The inspector value.</param>
    public void Accept(StateMachineVisitor inspector)
    {
        inspector.Visit(this);
    }

    /// <summary>
    /// Performs the probe operation.
    /// </summary>
    /// <param name="context">The operation context.</param>
    public void Probe(ProbeContext context)
    {
        context.CreateScope("schedule-faulted");
    }

    /// <summary>
    /// Performs the execute operation.
    /// </summary>
    /// <param name="context">The operation context.</param>
    /// <param name="next">The next value.</param>
    /// <returns>The result of the operation.</returns>
    public Task ExecuteAsync(BehaviorContext<TSaga> context, IBehavior<TSaga> next)
    {
        return next.ExecuteAsync(context);
    }

    /// <summary>
    /// Performs the execute operation.
    /// </summary>
    /// <typeparam name="T">The t type.</typeparam>
    /// <param name="context">The operation context.</param>
    /// <param name="next">The next value.</param>
    /// <returns>The result of the operation.</returns>
    public Task ExecuteAsync<T>(BehaviorContext<TSaga, T> context, IBehavior<TSaga, T> next)
        where T : class
    {
        return next.ExecuteAsync(context);
    }

    /// <summary>
    /// Performs the faulted operation.
    /// </summary>
    /// <typeparam name="T">The t type.</typeparam>
    /// <param name="context">The operation context.</param>
    /// <param name="next">The next value.</param>
    /// <returns>The result of the operation.</returns>
    public async Task FaultedAsync<T>(BehaviorExceptionContext<TSaga, T> context, IBehavior<TSaga> next)
        where T : Exception
    {
        if (context is BehaviorExceptionContext<TSaga, TException> exceptionContext)
            await ScheduleAsync(context, exceptionContext).ConfigureAwait(false);

        await next.FaultedAsync(context).ConfigureAwait(false);
    }

    /// <summary>
    /// Performs the faulted operation.
    /// </summary>
    /// <typeparam name="T">The t type.</typeparam>
    /// <typeparam name="TOtherException">The t other exception type.</typeparam>
    /// <param name="context">The operation context.</param>
    /// <param name="next">The next value.</param>
    /// <returns>The result of the operation.</returns>
    public async Task FaultedAsync<T, TOtherException>(BehaviorExceptionContext<TSaga, T, TOtherException> context, IBehavior<TSaga, T> next)
        where T : class
        where TOtherException : Exception
    {
        if (context is BehaviorExceptionContext<TSaga, TException> exceptionContext)
            await ScheduleAsync(context, exceptionContext).ConfigureAwait(false);

        await next.FaultedAsync(context).ConfigureAwait(false);
    }

    async Task ScheduleAsync<T>(BehaviorExceptionContext<TSaga, T> context, BehaviorExceptionContext<TSaga, TException> exceptionContext)
        where T : Exception
    {
        Guid? previousTokenId = _schedule.GetTokenId(context.Saga);

        var schedulerContext = context.GetPayload<MessageSchedulerContext>();

        ScheduledMessage<TMessage> message = await _messageFactory
            .UseAsync(exceptionContext, (ctx, s) => schedulerContext.ScheduleSendAsync(_timeProvider(ctx), s.Message, s.Pipe, ctx.CancellationToken))
            .ConfigureAwait(false);

        _schedule?.SetTokenId(context.Saga, message.TokenId);

        if (previousTokenId.HasValue)
        {
            Guid? messageTokenId = context.GetSchedulingTokenId();
            if (!messageTokenId.HasValue || previousTokenId.Value != messageTokenId.Value)
                await schedulerContext.CancelScheduledSendAsync(context.ReceiveContext.InputAddress, previousTokenId.Value).ConfigureAwait(false);
        }
    }
}


/// <summary>
/// Provides a faulted schedule activity implementation.
/// </summary>
/// <typeparam name="TSaga">The t saga type.</typeparam>
/// <typeparam name="TData">The t data type.</typeparam>
/// <typeparam name="TException">The t exception type.</typeparam>
/// <typeparam name="TMessage">The t message type.</typeparam>
public class FaultedScheduleActivity<TSaga, TData, TException, TMessage> :
    IStateMachineActivity<TSaga, TData>
    where TSaga : class, SagaStateMachineInstance
    where TData : class
    where TException : Exception
    where TMessage : class
{
    readonly ContextMessageFactory<BehaviorExceptionContext<TSaga, TData, TException>, TMessage> _messageFactory;
    readonly Schedule<TSaga, TMessage> _schedule;
    readonly ScheduleTimeExceptionProvider<TSaga, TData, TException> _timeProvider;

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="schedule">The schedule value.</param>
    /// <param name="timeProvider">The time provider value.</param>
    /// <param name="messageFactory">The message factory value.</param>
    public FaultedScheduleActivity(Schedule<TSaga, TMessage> schedule, ScheduleTimeExceptionProvider<TSaga, TData, TException> timeProvider,
        ContextMessageFactory<BehaviorExceptionContext<TSaga, TData, TException>, TMessage> messageFactory)
    {
        _messageFactory = messageFactory;
        _schedule = schedule;
        _timeProvider = timeProvider;
    }

    /// <summary>
    /// Performs the accept operation.
    /// </summary>
    /// <param name="inspector">The inspector value.</param>
    public void Accept(StateMachineVisitor inspector)
    {
        inspector.Visit(this);
    }

    /// <summary>
    /// Performs the probe operation.
    /// </summary>
    /// <param name="context">The operation context.</param>
    public void Probe(ProbeContext context)
    {
        context.CreateScope("schedule-faulted");
    }

    /// <summary>
    /// Performs the execute operation.
    /// </summary>
    /// <param name="context">The operation context.</param>
    /// <param name="next">The next value.</param>
    /// <returns>The result of the operation.</returns>
    public Task ExecuteAsync(BehaviorContext<TSaga, TData> context, IBehavior<TSaga, TData> next)
    {
        return next.ExecuteAsync(context);
    }

    /// <summary>
    /// Performs the faulted operation.
    /// </summary>
    /// <typeparam name="T">The t type.</typeparam>
    /// <param name="context">The operation context.</param>
    /// <param name="next">The next value.</param>
    /// <returns>The result of the operation.</returns>
    public async Task FaultedAsync<T>(BehaviorExceptionContext<TSaga, TData, T> context, IBehavior<TSaga, TData> next)
        where T : Exception
    {
        if (context is BehaviorExceptionContext<TSaga, TData, TException> exceptionContext)
        {
            Guid? previousTokenId = _schedule.GetTokenId(context.Saga);

            var schedulerContext = context.GetPayload<MessageSchedulerContext>();

            ScheduledMessage<TMessage> message = await _messageFactory
                .UseAsync(exceptionContext, (ctx, s) => schedulerContext.ScheduleSendAsync(_timeProvider(ctx), s.Message, s.Pipe, ctx.CancellationToken))
                .ConfigureAwait(false);

            _schedule?.SetTokenId(context.Saga, message.TokenId);

            if (previousTokenId.HasValue)
            {
                Guid? messageTokenId = context.GetSchedulingTokenId();
                if (!messageTokenId.HasValue || previousTokenId.Value != messageTokenId.Value)
                    await schedulerContext.CancelScheduledSendAsync(context.ReceiveContext.InputAddress, previousTokenId.Value).ConfigureAwait(false);
            }
        }

        await next.FaultedAsync(context).ConfigureAwait(false);
    }
}
