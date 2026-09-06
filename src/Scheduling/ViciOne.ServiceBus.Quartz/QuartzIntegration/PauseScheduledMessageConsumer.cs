using System;
using System.Threading.Tasks;
using Quartz;
using ViciOne.ServiceBus.Scheduling;

namespace ViciOne.ServiceBus.Quartz;

/// <summary>Pauses recurring Quartz triggers addressed by scheduling commands.</summary>
public class PauseScheduledMessageConsumer :
    IConsumer<PauseScheduledRecurringMessage>
{
    readonly ISchedulerFactory _schedulerFactory;

    /// <summary>Initializes the consumer with the factory used to resolve the active Quartz scheduler.</summary>
    /// <param name="schedulerFactory">The factory that resolves the active Quartz scheduler.</param>
    public PauseScheduledMessageConsumer(ISchedulerFactory schedulerFactory)
    {
        _schedulerFactory = schedulerFactory ?? throw new ArgumentNullException(nameof(schedulerFactory));
    }

    /// <summary>Pauses the recurring trigger identified by schedule group and identifier.</summary>
    /// <param name="context">The recurring pause command context.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public async Task ConsumeAsync(ConsumeContext<PauseScheduledRecurringMessage> context)
    {
        ArgumentNullException.ThrowIfNull(context);
        var scheduler = await _schedulerFactory.GetScheduler(context.CancellationToken).ConfigureAwait(false);

        var triggerKey = QuartzTriggerKey.ForRecurring(context.Message.ScheduleId, context.Message.ScheduleGroup);
        await scheduler.PauseTrigger(triggerKey, context.CancellationToken)
            .ConfigureAwait(false);

        LogContext.Debug?.Log("PauseScheduledRecurringMessage: {ScheduleId}/{ScheduleGroup} at {Timestamp}", context.Message.ScheduleId,
            context.Message.ScheduleGroup, context.Message.Timestamp);
    }
}
