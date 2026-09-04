using System;
using System.Threading.Tasks;
using Quartz;
using ViciOne.ServiceBus.Scheduling;

namespace ViciOne.ServiceBus.Quartz;

/// <summary>
/// Provides a resume scheduled message consumer implementation.
/// </summary>
public class ResumeScheduledMessageConsumer :
    IConsumer<ResumeScheduledRecurringMessage>
{
    readonly ISchedulerFactory _schedulerFactory;

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="schedulerFactory">The scheduler factory value.</param>
    public ResumeScheduledMessageConsumer(ISchedulerFactory schedulerFactory)
    {
        _schedulerFactory = schedulerFactory ?? throw new ArgumentNullException(nameof(schedulerFactory));
    }

    /// <summary>
    /// Consumes the message provided by the context.
    /// </summary>
    /// <param name="context">The operation context.</param>
    /// <returns>The result of the operation.</returns>
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
