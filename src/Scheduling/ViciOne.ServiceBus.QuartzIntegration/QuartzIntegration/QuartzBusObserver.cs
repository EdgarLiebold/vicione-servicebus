using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;
using Quartz;

namespace ViciOne.ServiceBus.QuartzIntegration;

/// <summary>
/// Used by container-based Quartz configurations, to start/stop Quartz along with the bus.
/// </summary>
public class QuartzBusObserver :
    IBusObserver
{
    readonly QuartzHostedServiceSettings _settings;
    readonly ISchedulerFactory _schedulerFactory;
    IScheduler? _scheduler;

    public QuartzBusObserver(ISchedulerFactory schedulerFactory, IOptions<QuartzHostedServiceOptions> options)
    {
        _schedulerFactory = schedulerFactory ?? throw new ArgumentNullException(nameof(schedulerFactory));
        ArgumentNullException.ThrowIfNull(options);
        _settings = QuartzHostedServiceSettings.From(options.Value);
    }

    public void PostCreate(IBus bus)
    {
    }

    public void CreateFaulted(Exception exception)
    {
    }

    public Task PreStart(IBus bus)
    {
        return Task.CompletedTask;
    }

    public async Task PostStart(IBus bus, Task<BusReady> busReady)
    {
        await busReady.ConfigureAwait(false);

        _scheduler = await _schedulerFactory.GetScheduler().ConfigureAwait(false);

        if (_settings.StartDelay.HasValue)
            await _scheduler.StartDelayed(_settings.StartDelay.Value).ConfigureAwait(false);
        else
            await _scheduler.Start().ConfigureAwait(false);
    }

    public Task StartFaulted(IBus bus, Exception exception)
    {
        return Task.CompletedTask;
    }

    public async Task PreStop(IBus bus)
    {
        if (_scheduler != null)
            await _scheduler.Standby().ConfigureAwait(false);
    }

    public async Task PostStop(IBus bus)
    {
        if (_scheduler != null)
            await _scheduler.Shutdown(_settings.WaitForJobsToComplete).ConfigureAwait(false);
    }

    public Task StopFaulted(IBus bus, Exception exception)
    {
        return Task.CompletedTask;
    }
}
