using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;
using Quartz;

namespace ViciOne.ServiceBus.Quartz;

/// <summary>Starts, pauses, and shuts down a container-registered Quartz scheduler with the bus lifecycle.</summary>
public class QuartzBusObserver :
    IBusObserver
{
    readonly QuartzHostedServiceSettings _settings;
    readonly ISchedulerFactory _schedulerFactory;
    IScheduler? _scheduler;

    /// <summary>Initializes an observer that coordinates a Quartz scheduler with the bus lifecycle.</summary>
    /// <param name="schedulerFactory">The factory that resolves the scheduler controlled by this observer.</param>
    /// <param name="options">The configured start delay and shutdown behavior.</param>
    public QuartzBusObserver(ISchedulerFactory schedulerFactory, IOptions<QuartzHostedServiceOptions> options)
    {
        _schedulerFactory = schedulerFactory ?? throw new ArgumentNullException(nameof(schedulerFactory));
        ArgumentNullException.ThrowIfNull(options);
        _settings = QuartzHostedServiceSettings.From(options.Value);
    }

    /// <summary>Performs no action after bus creation.</summary>
    /// <param name="bus">The created bus.</param>
    public void PostCreate(IBus bus)
    {
    }

    /// <summary>Performs no action when bus creation fails.</summary>
    /// <param name="exception">The bus-creation failure.</param>
    public void CreateFaulted(Exception exception)
    {
    }

    /// <summary>Performs no action before bus startup.</summary>
    /// <param name="bus">The bus being started.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public Task PreStartAsync(IBus bus)
    {
        return Task.CompletedTask;
    }

    /// <summary>Starts the scheduler after the bus reports readiness.</summary>
    /// <param name="bus">The bus whose lifecycle owns the scheduler.</param>
    /// <param name="busReady">The readiness task that gates scheduler startup.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public async Task PostStartAsync(IBus bus, Task<BusReady> busReady)
    {
        await busReady.ConfigureAwait(false);

        _scheduler = await _schedulerFactory.GetScheduler().ConfigureAwait(false);

        if (_settings.StartDelay.HasValue)
            await _scheduler.StartDelayed(_settings.StartDelay.Value).ConfigureAwait(false);
        else
            await _scheduler.Start().ConfigureAwait(false);
    }

    /// <summary>Performs no action when bus startup fails.</summary>
    /// <param name="bus">The bus that failed to start.</param>
    /// <param name="exception">The startup failure.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public Task StartFaultedAsync(IBus bus, Exception exception)
    {
        return Task.CompletedTask;
    }

    /// <summary>Places the scheduler in standby before bus shutdown.</summary>
    /// <param name="bus">The bus whose lifecycle owns the scheduler.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public async Task PreStopAsync(IBus bus)
    {
        if (_scheduler != null)
            await _scheduler.Standby().ConfigureAwait(false);
    }

    /// <summary>Shuts down the scheduler after the bus stops.</summary>
    /// <param name="bus">The bus whose lifecycle owns the scheduler.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public async Task PostStopAsync(IBus bus)
    {
        if (_scheduler != null)
            await _scheduler.Shutdown(_settings.WaitForJobsToComplete).ConfigureAwait(false);
    }

    /// <summary>Performs no action when bus shutdown fails.</summary>
    /// <param name="bus">The bus that failed to stop.</param>
    /// <param name="exception">The shutdown failure.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public Task StopFaultedAsync(IBus bus, Exception exception)
    {
        return Task.CompletedTask;
    }
}
