using System;
using System.Threading;
using System.Threading.Tasks;
using Quartz;
using ViciOne.ServiceBus.Advanced;
using ViciOne.ServiceBus.Quartz.Runtime;
using ViciOne.ServiceBus.Scheduling;

namespace ViciOne.ServiceBus.Quartz.Consumers;

/// <summary>Removes one-time and recurring Quartz triggers addressed by cancellation commands.</summary>
internal sealed class CancelScheduledMessageConsumer<TBus> :
    IConsumer<CancelScheduledMessage>,
    IConsumer<CancelScheduledRecurringMessage>
    where TBus : class, IBus
{
    readonly QuartzSchedulerBinding<TBus>? _binding;
    readonly ISchedulerFactory? _schedulerFactory;
    readonly string _schedulerNamespace;

    /// <summary>Initializes the consumer with the factory used to resolve the active Quartz scheduler.</summary>
    /// <param name="binding">The bus-specific scheduler binding.</param>
    public CancelScheduledMessageConsumer(QuartzSchedulerBinding<TBus> binding)
    {
        _binding = binding ?? throw new ArgumentNullException(nameof(binding));
        _schedulerNamespace = binding.Settings.SchedulerNamespace;
    }

    internal CancelScheduledMessageConsumer(ISchedulerFactory schedulerFactory, string schedulerNamespace)
    {
        _schedulerFactory = schedulerFactory ?? throw new ArgumentNullException(nameof(schedulerFactory));
        ArgumentException.ThrowIfNullOrWhiteSpace(schedulerNamespace);
        _schedulerNamespace = schedulerNamespace;
    }

    /// <summary>Unschedules the one-time trigger identified by the command token.</summary>
    /// <param name="context">The one-time cancellation command context.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public async Task ConsumeAsync(ConsumeContext<CancelScheduledMessage> context)
    {
        ArgumentNullException.ThrowIfNull(context);
        string triggerName = context.Message.TokenId.ToString("N");
        var triggerKey = new TriggerKey(triggerName, _schedulerNamespace);

        var scheduler = await GetSchedulerAsync(context.CancellationToken).ConfigureAwait(false);

        bool wasUnscheduled = await scheduler.UnscheduleJob(triggerKey, context.CancellationToken).ConfigureAwait(false);

        if (wasUnscheduled)
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
        var scheduler = await GetSchedulerAsync(context.CancellationToken).ConfigureAwait(false);

        var triggerKey = QuartzTriggerKey.ForRecurring(context.Message.ScheduleId, context.Message.ScheduleGroup, _schedulerNamespace);
        bool wasUnscheduled = await scheduler.UnscheduleJob(triggerKey, context.CancellationToken)
            .ConfigureAwait(false);

        if (wasUnscheduled)
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

    private ValueTask<IScheduler> GetSchedulerAsync(CancellationToken cancellationToken)
    {
        return _binding is not null
            ? _binding.GetSchedulerAsync(cancellationToken)
            : _schedulerFactory!.GetScheduler(cancellationToken);
    }
}
