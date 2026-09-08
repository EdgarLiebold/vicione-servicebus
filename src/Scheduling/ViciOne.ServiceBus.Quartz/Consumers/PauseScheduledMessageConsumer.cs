using System;
using System.Threading;
using System.Threading.Tasks;
using Quartz;
using ViciOne.ServiceBus.Advanced;
using ViciOne.ServiceBus.Quartz.Runtime;
using ViciOne.ServiceBus.Scheduling;

namespace ViciOne.ServiceBus.Quartz.Consumers;

/// <summary>Pauses recurring Quartz triggers addressed by scheduling commands.</summary>
internal sealed class PauseScheduledMessageConsumer<TBus> :
    IConsumer<PauseScheduledRecurringMessage>
    where TBus : class, IBus
{
    readonly QuartzSchedulerBinding<TBus>? _binding;
    readonly ISchedulerFactory? _schedulerFactory;
    readonly string _schedulerNamespace;

    /// <summary>Initializes the consumer with the factory used to resolve the active Quartz scheduler.</summary>
    /// <param name="binding">The bus-specific scheduler binding.</param>
    public PauseScheduledMessageConsumer(QuartzSchedulerBinding<TBus> binding)
    {
        _binding = binding ?? throw new ArgumentNullException(nameof(binding));
        _schedulerNamespace = binding.Settings.SchedulerNamespace;
    }

    internal PauseScheduledMessageConsumer(ISchedulerFactory schedulerFactory, string schedulerNamespace)
    {
        _schedulerFactory = schedulerFactory ?? throw new ArgumentNullException(nameof(schedulerFactory));
        ArgumentException.ThrowIfNullOrWhiteSpace(schedulerNamespace);
        _schedulerNamespace = schedulerNamespace;
    }

    /// <summary>Pauses the recurring trigger identified by schedule group and identifier.</summary>
    /// <param name="context">The recurring pause command context.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public async Task ConsumeAsync(ConsumeContext<PauseScheduledRecurringMessage> context)
    {
        ArgumentNullException.ThrowIfNull(context);
        var scheduler = await GetSchedulerAsync(context.CancellationToken).ConfigureAwait(false);

        var triggerKey = QuartzTriggerKey.ForRecurring(context.Message.ScheduleId, context.Message.ScheduleGroup, _schedulerNamespace);
        await scheduler.PauseTrigger(triggerKey, context.CancellationToken)
            .ConfigureAwait(false);

        LogContext.Debug?.Log("PauseScheduledRecurringMessage: {ScheduleId}/{ScheduleGroup} at {Timestamp}", context.Message.ScheduleId,
            context.Message.ScheduleGroup, context.Message.Timestamp);
    }

    private ValueTask<IScheduler> GetSchedulerAsync(CancellationToken cancellationToken)
    {
        return _binding is not null
            ? _binding.GetSchedulerAsync(cancellationToken)
            : _schedulerFactory!.GetScheduler(cancellationToken);
    }
}
