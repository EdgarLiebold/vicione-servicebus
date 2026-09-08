using System;
using System.Threading.Tasks;
using Quartz;
using ViciOne.ServiceBus.Advanced;
using ViciOne.ServiceBus.Advanced.Observers;
using ViciOne.ServiceBus.Providers.Configuration;

namespace ViciOne.ServiceBus.Quartz.Runtime;

/// <summary>Coordinates a directly configured Quartz scheduler with the bus lifecycle.</summary>
internal sealed class DirectQuartzSchedulerLifecycleObserver :
    IBusObserver
{
    readonly QuartzSchedulerSettings _settings;
    readonly Uri _schedulerEndpointAddress;
    IBus? _attachedBus;
    IScheduler? _scheduler;

    /// <summary>Creates the bus observer to initialize the Quartz scheduler.</summary>
    /// <param name="settings">Validated immutable scheduler settings.</param>
    public DirectQuartzSchedulerLifecycleObserver(QuartzSchedulerSettings settings)
    {
        _settings = settings ?? throw new ArgumentNullException(nameof(settings));
        _schedulerEndpointAddress = new Uri($"queue:{settings.QueueName}");
    }

    public void PostCreate(IBus bus)
    {
    }

    public void CreateFaulted(Exception exception)
    {
    }

    public async Task PreStartAsync(IBus bus)
    {
        ArgumentNullException.ThrowIfNull(bus);
        LogContext.Debug?.Log("Creating Quartz Scheduler: {InputAddress}", _schedulerEndpointAddress);

        IScheduler scheduler = await _settings.SchedulerFactory.GetScheduler().ConfigureAwait(false);
        if (scheduler.Status == SchedulerStatus.Shutdown)
        {
            throw new ConfigurationException(ConfigurationMessages.Create(
                "Quartz scheduling",
                typeof(IBus).FullName ?? nameof(IBus),
                $"The scheduler for endpoint '{_schedulerEndpointAddress}' has been shut down and cannot be restarted",
                "Provide a live scheduler factory or create a new in-memory scheduler lease"));
        }

        Attach(scheduler, bus, _settings.TimeProvider);
        _scheduler = scheduler;
        _attachedBus = bus;
    }

    public async Task PostStartAsync(IBus bus, Task<BusReady> busReady)
    {
        ArgumentNullException.ThrowIfNull(bus);
        ArgumentNullException.ThrowIfNull(busReady);
        if (!_settings.StartScheduler)
        {
            LogContext.Debug?.Log("Quartz Scheduler: {InputAddress} ({Name}/{InstanceId}) initialized, but not started",
                _schedulerEndpointAddress,
                _scheduler?.SchedulerName,
                _scheduler?.SchedulerInstanceId);

            return;
        }

        LogContext.Debug?.Log("Quartz Scheduler Starting: {InputAddress} ({Name}/{InstanceId})", _schedulerEndpointAddress, _scheduler?.SchedulerName,
            _scheduler?.SchedulerInstanceId);

        await busReady.ConfigureAwait(false);

        if (_settings.StartDelay.HasValue)
            await _scheduler!.StartDelayed(_settings.StartDelay.Value).ConfigureAwait(false);
        else
            await _scheduler!.Start().ConfigureAwait(false);

        LogContext.Debug?.Log("Quartz Scheduler Started: {InputAddress} ({Name}/{InstanceId})", _schedulerEndpointAddress, _scheduler.SchedulerName,
            _scheduler.SchedulerInstanceId);
    }

    public Task StartFaultedAsync(IBus bus, Exception exception)
    {
        return StandbyAndDetachAsync();
    }

    public async Task PreStopAsync(IBus bus)
    {
        IScheduler? scheduler = _scheduler;
        if (scheduler is null)
            return;

        try
        {
            if (_settings.StartScheduler)
                await scheduler.Standby().ConfigureAwait(false);

            LogContext.Debug?.Log("Quartz Scheduler Paused: {InputAddress} ({Name}/{InstanceId})", _schedulerEndpointAddress, scheduler.SchedulerName,
                scheduler.SchedulerInstanceId);
        }
        finally
        {
            Detach(scheduler, _attachedBus);
            _attachedBus = null;
            _scheduler = null;
        }
    }

    public Task PostStopAsync(IBus bus)
    {
        return Task.CompletedTask;
    }

    public Task StopFaultedAsync(IBus bus, Exception exception)
    {
        return StandbyAndDetachAsync();
    }

    private async Task StandbyAndDetachAsync()
    {
        IScheduler? scheduler = _scheduler;
        if (scheduler is null)
            return;

        _scheduler = null;
        try
        {
            if (_settings.StartScheduler)
                await scheduler.Standby().ConfigureAwait(false);
        }
        finally
        {
            Detach(scheduler, _attachedBus);
            _attachedBus = null;
        }
    }

    private static void Attach(IScheduler scheduler, IBus bus, TimeProvider timeProvider)
    {
        lock (scheduler.Context)
        {
            if (scheduler.Context.TryGetValue(QuartzSchedulerContextKeys.Bus, out object? owner)
                && !ReferenceEquals(owner, bus))
            {
                throw new ConfigurationException(ConfigurationMessages.Create(
                    "Quartz scheduling",
                    typeof(IBus).FullName ?? nameof(IBus),
                    $"Scheduler '{scheduler.SchedulerName}' is already attached to another bus",
                    "Provide a distinct scheduler factory for each bus"));
            }

            scheduler.Context[QuartzSchedulerContextKeys.Bus] = bus;
            scheduler.Context[QuartzSchedulerContextKeys.TimeProvider] = timeProvider;
        }
    }

    private static void Detach(IScheduler scheduler, IBus? bus)
    {
        lock (scheduler.Context)
        {
            if (!scheduler.Context.TryGetValue(QuartzSchedulerContextKeys.Bus, out object? owner)
                || !ReferenceEquals(owner, bus))
            {
                return;
            }

            scheduler.Context.Remove(QuartzSchedulerContextKeys.Bus);
            scheduler.Context.Remove(QuartzSchedulerContextKeys.TimeProvider);
        }
    }
}
