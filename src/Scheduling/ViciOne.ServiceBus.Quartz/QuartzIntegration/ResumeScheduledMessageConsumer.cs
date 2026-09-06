using System;
using System.Threading.Tasks;
using Quartz;
using ViciOne.ServiceBus.Scheduling;

namespace ViciOne.ServiceBus.Quartz;

/// <summary>Resumes recurring Quartz triggers addressed by scheduling commands.</summary>
public class ResumeScheduledMessageConsumer :
    IConsumer<ResumeScheduledRecurringMessage>
{
    readonly ISchedulerFactory _schedulerFactory;

    /// <summary>Initializes the consumer with the factory used to resolve the active Quartz scheduler.</summary>
    /// <param name="schedulerFactory">The factory that resolves the active Quartz scheduler.</param>
    public ResumeScheduledMessageConsumer(ISchedulerFactory schedulerFactory)
    {
        _schedulerFactory = schedulerFactory ?? throw new ArgumentNullException(nameof(schedulerFactory));
    }

    /// <summary>Resumes the recurring trigger identified by schedule group and identifier.</summary>
    /// <param name="context">The recurring resume command context.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public async Task ConsumeAsync(ConsumeContext<ResumeScheduledRecurringMessage> context)
    {
        ArgumentNullException.ThrowIfNull(context);
        var scheduler = await _schedulerFactory.GetScheduler(context.CancellationToken).ConfigureAwait(false);

        var triggerKey = QuartzTriggerKey.ForRecurring(context.Message.ScheduleId, context.Message.ScheduleGroup);
        await scheduler.ResumeTrigger(triggerKey, context.CancellationToken)
            .ConfigureAwait(false);

        LogContext.Debug?.Log("ResumeScheduledRecurringMessage: {ScheduleId}/{ScheduleGroup} at {Timestamp}", context.Message.ScheduleId,
            context.Message.ScheduleGroup, context.Message.Timestamp);
    }
}
