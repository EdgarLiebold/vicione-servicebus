using System;
using System.Threading.Tasks;

namespace ViciOne.ServiceBus.SagaStateMachine;

/// <summary>
/// Provides a faulted unschedule activity implementation.
/// </summary>
/// <typeparam name="TSaga">The t saga type.</typeparam>
public class FaultedUnscheduleActivity<TSaga> :
    IStateMachineActivity<TSaga>
    where TSaga : class, SagaStateMachineInstance
{
    readonly Schedule<TSaga> _schedule;

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="schedule">The schedule value.</param>
    public FaultedUnscheduleActivity(Schedule<TSaga> schedule)
    {
        _schedule = schedule;
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
        context.CreateScope("unschedule-faulted");
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
    /// <typeparam name="TException">The t exception type.</typeparam>
    /// <param name="context">The operation context.</param>
    /// <param name="next">The next value.</param>
    /// <returns>The result of the operation.</returns>
    public async Task FaultedAsync<TException>(BehaviorExceptionContext<TSaga, TException> context, IBehavior<TSaga> next)
        where TException : Exception
    {
        await FaultedAsync(context).ConfigureAwait(false);

        await next.FaultedAsync(context).ConfigureAwait(false);
    }

    /// <summary>
    /// Performs the faulted operation.
    /// </summary>
    /// <typeparam name="T">The t type.</typeparam>
    /// <typeparam name="TException">The t exception type.</typeparam>
    /// <param name="context">The operation context.</param>
    /// <param name="next">The next value.</param>
    /// <returns>The result of the operation.</returns>
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
