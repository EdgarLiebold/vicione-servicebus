using System;
using System.Threading.Tasks;
using Quartz;
using ViciOne.ServiceBus.Scheduling;

namespace ViciOne.ServiceBus.Quartz;

/// <summary>Removes one-time and recurring Quartz triggers addressed by cancellation commands.</summary>
public class CancelScheduledMessageConsumer :
    IConsumer<CancelScheduledMessage>,
    IConsumer<CancelScheduledRecurringMessage>
{
    readonly ISchedulerFactory _schedulerFactory;

    /// <summary>Initializes the consumer with the factory used to resolve the active Quartz scheduler.</summary>
    /// <param name="schedulerFactory">The factory that resolves the active Quartz scheduler.</param>
    public CancelScheduledMessageConsumer(ISchedulerFactory schedulerFactory)
    {
        _schedulerFactory = schedulerFactory ?? throw new ArgumentNullException(nameof(schedulerFactory));
    }

    /// <summary>Unschedules the one-time trigger identified by the command token.</summary>
    /// <param name="context">The one-time cancellation command context.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public async Task ConsumeAsync(ConsumeContext<CancelScheduledMessage> context)
    {
        ArgumentNullException.ThrowIfNull(context);
        var correlationId = context.Message.TokenId.ToString("N");
        var triggerKey = new TriggerKey(correlationId);

        var scheduler = await _schedulerFactory.GetScheduler(context.CancellationToken).ConfigureAwait(false);

        var unscheduleJob = await scheduler.UnscheduleJob(triggerKey, context.CancellationToken).ConfigureAwait(false);

        if (unscheduleJob)
            LogContext.Debug?.Log("Canceled Scheduled Message: {Id} at {Timestamp}", triggerKey, context.Message.Timestamp);
        else
            LogContext.Debug?.Log("CancelScheduledMessage: no message found for {Id}", triggerKey);
    }

    /// <summary>Unschedules the recurring trigger identified by schedule group and identifier.</summary>
    /// <param name="context">The recurring cancellation command context.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public async Task ConsumeAsync(ConsumeContext<CancelScheduledRecurringMessage> context)
    {
        ArgumentNullException.ThrowIfNull(context);
        var scheduler = await _schedulerFactory.GetScheduler(context.CancellationToken).ConfigureAwait(false);

        var triggerKey = QuartzTriggerKey.ForRecurring(context.Message.ScheduleId, context.Message.ScheduleGroup);
        var unscheduledJob = await scheduler.UnscheduleJob(triggerKey, context.CancellationToken)
            .ConfigureAwait(false);

        if (unscheduledJob)
        {
            LogContext.Debug?.Log("CancelRecurringScheduledMessage: {ScheduleId}/{ScheduleGroup} at {Timestamp}", context.Message.ScheduleId,
                context.Message.ScheduleGroup, context.Message.Timestamp);
        }
        else
        {
            LogContext.Debug?.Log("CancelRecurringScheduledMessage: no message found {ScheduleId}/{ScheduleGroup}", context.Message.ScheduleId,
                context.Message.ScheduleGroup);
        }
    }
}
