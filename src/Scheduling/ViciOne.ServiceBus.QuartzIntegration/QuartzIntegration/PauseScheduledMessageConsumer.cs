using System;
using System.Threading.Tasks;
using Quartz;
using ViciOne.ServiceBus.Scheduling;

namespace ViciOne.ServiceBus.QuartzIntegration;

public class PauseScheduledMessageConsumer :
    IConsumer<PauseScheduledRecurringMessage>
{
    readonly ISchedulerFactory _schedulerFactory;

    public PauseScheduledMessageConsumer(ISchedulerFactory schedulerFactory)
    {
        _schedulerFactory = schedulerFactory ?? throw new ArgumentNullException(nameof(schedulerFactory));
    }

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
