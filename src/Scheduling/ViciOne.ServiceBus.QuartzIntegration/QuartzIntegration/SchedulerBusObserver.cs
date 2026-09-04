using System;
using System.Threading.Tasks;
using Quartz;

namespace ViciOne.ServiceBus.QuartzIntegration;

/// <summary>
/// Used to start and stop an in-memory scheduler using Quartz
/// </summary>
internal sealed class SchedulerBusObserver :
    IBusObserver
{
    readonly QuartzSchedulerSettings _settings;
    readonly Uri _schedulerEndpointAddress;
    IScheduler? _scheduler;

    /// <summary>
    /// Creates the bus observer to initialize the Quartz scheduler.
    /// </summary>
    /// <param name="settings">Validated immutable scheduler settings.</param>
    public SchedulerBusObserver(QuartzSchedulerSettings settings)
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

    public async Task PreStart(IBus bus)
    {
        LogContext.Debug?.Log("Creating Quartz Scheduler: {InputAddress}", _schedulerEndpointAddress);

        _scheduler = await _settings.SchedulerFactory.GetScheduler().ConfigureAwait(false);

        _scheduler.Context[ScheduledMessageJob.BusContextKey] = bus;
        _scheduler.Context[ScheduledMessageJob.TimeProviderContextKey] = _settings.TimeProvider;
    }

    public async Task PostStart(IBus bus, Task<BusReady> busReady)
    {
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

        await _scheduler!.Start().ConfigureAwait(false);

        LogContext.Debug?.Log("Quartz Scheduler Started: {InputAddress} ({Name}/{InstanceId})", _schedulerEndpointAddress, _scheduler.SchedulerName,
            _scheduler.SchedulerInstanceId);
    }

    public Task StartFaulted(IBus bus, Exception exception)
    {
        return Task.CompletedTask;
    }

    public async Task PreStop(IBus bus)
    {
        if (!_settings.StartScheduler)
            return;

        await _scheduler!.Standby().ConfigureAwait(false);

        LogContext.Debug?.Log("Quartz Scheduler Paused: {InputAddress} ({Name}/{InstanceId})", _schedulerEndpointAddress, _scheduler.SchedulerName,
            _scheduler.SchedulerInstanceId);
    }

    public async Task PostStop(IBus bus)
    {
        try
        {
            await _scheduler!.Shutdown().ConfigureAwait(false);

            LogContext.Debug?.Log("Quartz Scheduler Stopped: {InputAddress} ({Name}/{InstanceId})", _schedulerEndpointAddress, _scheduler.SchedulerName,
                _scheduler.SchedulerInstanceId);
        }
        finally
        {
            _scheduler!.Context.Remove(ScheduledMessageJob.BusContextKey);
            _scheduler.Context.Remove(ScheduledMessageJob.TimeProviderContextKey);
        }
    }

    public Task StopFaulted(IBus bus, Exception exception)
    {
        return Task.CompletedTask;
    }
}
