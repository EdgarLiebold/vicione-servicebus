using System;
using System.Threading.Tasks;

namespace ViciOne.ServiceBus.SagaStateMachine;

/// <summary>Schedules a message when an untyped state-machine event faults with a selected exception.</summary>
/// <typeparam name="TSaga">The saga state type.</typeparam>
/// <typeparam name="TException">The exception type that triggers scheduling.</typeparam>
/// <typeparam name="TMessage">The scheduled message type.</typeparam>
internal sealed class FaultedScheduleActivity<TSaga, TException, TMessage> :
    IStateMachineActivity<TSaga>
    where TSaga : class, ISagaStateMachineInstance
    where TException : Exception
    where TMessage : class
{
    readonly ContextMessageFactory<IBehaviorExceptionContext<TSaga, TException>, TMessage> _messageFactory;
    readonly ISchedule<TSaga, TMessage> _schedule;
    readonly ScheduleTimeExceptionProvider<TSaga, TException> _timeProvider;

    /// <summary>Creates a fault activity for one saga schedule.</summary>
    /// <param name="schedule">The schedule whose token is stored on the saga.</param>
    /// <param name="timeProvider">The function that selects the due time from the fault context.</param>
    /// <param name="messageFactory">The factory that creates the scheduled message and send pipe.</param>
    public FaultedScheduleActivity(ISchedule<TSaga, TMessage> schedule, ScheduleTimeExceptionProvider<TSaga, TException> timeProvider,
        ContextMessageFactory<IBehaviorExceptionContext<TSaga, TException>, TMessage> messageFactory)
    {
        _schedule = schedule ?? throw new ArgumentNullException(nameof(schedule));
        _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
        _messageFactory = messageFactory ?? throw new ArgumentNullException(nameof(messageFactory));
    }

    /// <summary>Exposes this activity to a state-machine visitor.</summary>
    /// <param name="inspector">The visitor receiving the activity.</param>
    public void Accept(IStateMachineVisitor inspector)
    {
        ArgumentNullException.ThrowIfNull(inspector);
        inspector.Visit(this);
    }

    /// <summary>Adds the fault-scheduling activity to the diagnostic graph.</summary>
    /// <param name="context">The diagnostic context to populate.</param>
    public void Probe(ProbeContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        context.CreateScope("schedule-faulted");
    }

    /// <summary>Leaves the success path unchanged and invokes the remaining behavior.</summary>
    /// <param name="context">The successful saga behavior context.</param>
    /// <param name="next">The remaining behavior.</param>
    /// <returns>A task that completes after the remaining behavior.</returns>
    public Task ExecuteAsync(IBehaviorContext<TSaga> context, IBehavior<TSaga> next)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(next);
        return next.ExecuteAsync(context);
    }

    /// <summary>Leaves a successful data-event path unchanged and invokes the remaining behavior.</summary>
    /// <typeparam name="T">The event data type.</typeparam>
    /// <param name="context">The successful saga and event data.</param>
    /// <param name="next">The remaining data-event behavior.</param>
    /// <returns>A task that completes after the remaining behavior.</returns>
    public Task ExecuteAsync<T>(IBehaviorContext<TSaga, T> context, IBehavior<TSaga, T> next)
        where T : class
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(next);
        return next.ExecuteAsync(context);
    }

    /// <summary>Schedules the message for matching faults and then continues fault propagation.</summary>
    /// <typeparam name="T">The observed exception type.</typeparam>
    /// <param name="context">The faulted saga behavior.</param>
    /// <param name="next">The remaining fault behavior.</param>
    /// <returns>A task that completes after scheduling and fault propagation.</returns>
    public async Task FaultedAsync<T>(IBehaviorExceptionContext<TSaga, T> context, IBehavior<TSaga> next)
        where T : Exception
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(next);
        if (context is IBehaviorExceptionContext<TSaga, TException> exceptionContext)
            await ScheduleAsync(context, exceptionContext).ConfigureAwait(false);

        await next.FaultedAsync(context).ConfigureAwait(false);
    }

    /// <summary>Schedules the message for matching faults raised while handling event data.</summary>
    /// <typeparam name="T">The event data type.</typeparam>
    /// <typeparam name="TOtherException">The observed exception type.</typeparam>
    /// <param name="context">The faulted saga and event data.</param>
    /// <param name="next">The remaining data-event fault behavior.</param>
    /// <returns>A task that completes after scheduling and fault propagation.</returns>
    public async Task FaultedAsync<T, TOtherException>(IBehaviorExceptionContext<TSaga, T, TOtherException> context, IBehavior<TSaga, T> next)
        where T : class
        where TOtherException : Exception
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(next);
        if (context is IBehaviorExceptionContext<TSaga, TException> exceptionContext)
            await ScheduleAsync(context, exceptionContext).ConfigureAwait(false);

        await next.FaultedAsync(context).ConfigureAwait(false);
    }

    async Task ScheduleAsync<T>(IBehaviorExceptionContext<TSaga, T> context, IBehaviorExceptionContext<TSaga, TException> exceptionContext)
        where T : Exception
    {
        Guid? previousTokenId = _schedule.GetTokenId(context.Saga);

        var schedulerContext = context.GetPayload<MessageSchedulerContext>();

        ScheduledMessage<TMessage> message = await _messageFactory
            .UseAsync(exceptionContext, (ctx, s) => schedulerContext.ScheduleSendAsync(_timeProvider(ctx), s.Message, s.Pipe, ctx.CancellationToken))
            .ConfigureAwait(false);

        _schedule.SetTokenId(context.Saga, message.TokenId);

        if (previousTokenId.HasValue)
        {
            Guid? messageTokenId = context.GetSchedulingTokenId();
            if (!messageTokenId.HasValue || previousTokenId.Value != messageTokenId.Value)
                await schedulerContext.CancelScheduledSendAsync(
                        context.ReceiveContext.InputAddress,
                        previousTokenId.Value,
                        context.CancellationToken)
                    .ConfigureAwait(false);
        }
    }
}


/// <summary>Schedules a message when a data event faults with a selected exception.</summary>
/// <typeparam name="TSaga">The saga state type.</typeparam>
/// <typeparam name="TData">The event data type.</typeparam>
/// <typeparam name="TException">The exception type that triggers scheduling.</typeparam>
/// <typeparam name="TMessage">The scheduled message type.</typeparam>
internal sealed class FaultedScheduleActivity<TSaga, TData, TException, TMessage> :
    IStateMachineActivity<TSaga, TData>
    where TSaga : class, ISagaStateMachineInstance
    where TData : class
    where TException : Exception
    where TMessage : class
{
    readonly ContextMessageFactory<IBehaviorExceptionContext<TSaga, TData, TException>, TMessage> _messageFactory;
    readonly ISchedule<TSaga, TMessage> _schedule;
    readonly ScheduleTimeExceptionProvider<TSaga, TData, TException> _timeProvider;

    /// <summary>Creates a data-event fault activity for one saga schedule.</summary>
    /// <param name="schedule">The schedule whose token is stored on the saga.</param>
    /// <param name="timeProvider">The function that selects the due time from the fault context.</param>
    /// <param name="messageFactory">The factory that creates the scheduled message and send pipe.</param>
    public FaultedScheduleActivity(ISchedule<TSaga, TMessage> schedule, ScheduleTimeExceptionProvider<TSaga, TData, TException> timeProvider,
        ContextMessageFactory<IBehaviorExceptionContext<TSaga, TData, TException>, TMessage> messageFactory)
    {
        _schedule = schedule ?? throw new ArgumentNullException(nameof(schedule));
        _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
        _messageFactory = messageFactory ?? throw new ArgumentNullException(nameof(messageFactory));
    }

    /// <summary>Exposes this activity to a state-machine visitor.</summary>
    /// <param name="inspector">The visitor receiving the activity.</param>
    public void Accept(IStateMachineVisitor inspector)
    {
        ArgumentNullException.ThrowIfNull(inspector);
        inspector.Visit(this);
    }

    /// <summary>Adds the fault-scheduling activity to the diagnostic graph.</summary>
    /// <param name="context">The diagnostic context to populate.</param>
    public void Probe(ProbeContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        context.CreateScope("schedule-faulted");
    }

    /// <summary>Leaves the success path unchanged and invokes the remaining data-event behavior.</summary>
    /// <param name="context">The successful saga and event data.</param>
    /// <param name="next">The remaining data-event behavior.</param>
    /// <returns>A task that completes after the remaining behavior.</returns>
    public Task ExecuteAsync(IBehaviorContext<TSaga, TData> context, IBehavior<TSaga, TData> next)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(next);
        return next.ExecuteAsync(context);
    }

    /// <summary>Schedules the message for matching data-event faults and then continues fault propagation.</summary>
    /// <typeparam name="T">The observed exception type.</typeparam>
    /// <param name="context">The faulted saga and event data.</param>
    /// <param name="next">The remaining data-event fault behavior.</param>
    /// <returns>A task that completes after scheduling and fault propagation.</returns>
    public async Task FaultedAsync<T>(IBehaviorExceptionContext<TSaga, TData, T> context, IBehavior<TSaga, TData> next)
        where T : Exception
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(next);
        if (context is IBehaviorExceptionContext<TSaga, TData, TException> exceptionContext)
        {
            Guid? previousTokenId = _schedule.GetTokenId(context.Saga);

            var schedulerContext = context.GetPayload<MessageSchedulerContext>();

            ScheduledMessage<TMessage> message = await _messageFactory
                .UseAsync(exceptionContext, (ctx, s) => schedulerContext.ScheduleSendAsync(_timeProvider(ctx), s.Message, s.Pipe, ctx.CancellationToken))
                .ConfigureAwait(false);

            _schedule.SetTokenId(context.Saga, message.TokenId);

            if (previousTokenId.HasValue)
            {
                Guid? messageTokenId = context.GetSchedulingTokenId();
                if (!messageTokenId.HasValue || previousTokenId.Value != messageTokenId.Value)
                    await schedulerContext.CancelScheduledSendAsync(
                            context.ReceiveContext.InputAddress,
                            previousTokenId.Value,
                            context.CancellationToken)
                        .ConfigureAwait(false);
            }
        }

        await next.FaultedAsync(context).ConfigureAwait(false);
    }
}
