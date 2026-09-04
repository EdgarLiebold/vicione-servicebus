using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;
using Quartz;

namespace ViciOne.ServiceBus.Quartz;

/// <summary>
/// Used by container-based Quartz configurations, to start/stop Quartz along with the bus.
/// </summary>
public class QuartzBusObserver :
    IBusObserver
{
    readonly QuartzHostedServiceSettings _settings;
    readonly ISchedulerFactory _schedulerFactory;
    IScheduler? _scheduler;

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="schedulerFactory">The scheduler factory value.</param>
    /// <param name="options">The options value.</param>
    public QuartzBusObserver(ISchedulerFactory schedulerFactory, IOptions<QuartzHostedServiceOptions> options)
    {
        _schedulerFactory = schedulerFactory ?? throw new ArgumentNullException(nameof(schedulerFactory));
        ArgumentNullException.ThrowIfNull(options);
        _settings = QuartzHostedServiceSettings.From(options.Value);
    }

    /// <summary>
    /// Performs the post create operation.
    /// </summary>
    /// <param name="bus">The bus value.</param>
    public void PostCreate(IBus bus)
    {
    }

    /// <summary>
    /// Creates faulted.
    /// </summary>
    /// <param name="exception">The exception associated with the operation.</param>
    public void CreateFaulted(Exception exception)
    {
    }

    /// <summary>
    /// Performs the pre start operation.
    /// </summary>
    /// <param name="bus">The bus value.</param>
    /// <returns>The result of the operation.</returns>
    public Task PreStartAsync(IBus bus)
    {
        return Task.CompletedTask;
    }

    /// <summary>
    /// Performs the post start operation.
    /// </summary>
    /// <param name="bus">The bus value.</param>
    /// <param name="busReady">The bus ready value.</param>
    /// <returns>The result of the operation.</returns>
    public async Task PostStartAsync(IBus bus, Task<BusReady> busReady)
    {
        await busReady.ConfigureAwait(false);

        _scheduler = await _schedulerFactory.GetScheduler().ConfigureAwait(false);

        if (_settings.StartDelay.HasValue)
            await _scheduler.StartDelayed(_settings.StartDelay.Value).ConfigureAwait(false);
        else
            await _scheduler.Start().ConfigureAwait(false);
    }

    /// <summary>
    /// Starts faulted.
    /// </summary>
    /// <param name="bus">The bus value.</param>
    /// <param name="exception">The exception associated with the operation.</param>
    /// <returns>The result of the operation.</returns>
    public Task StartFaultedAsync(IBus bus, Exception exception)
    {
        return Task.CompletedTask;
    }

    /// <summary>
    /// Performs the pre stop operation.
    /// </summary>
    /// <param name="bus">The bus value.</param>
    /// <returns>The result of the operation.</returns>
    public async Task PreStopAsync(IBus bus)
    {
        if (_scheduler != null)
            await _scheduler.Standby().ConfigureAwait(false);
    }

    /// <summary>
    /// Performs the post stop operation.
    /// </summary>
    /// <param name="bus">The bus value.</param>
    /// <returns>The result of the operation.</returns>
    public async Task PostStopAsync(IBus bus)
    {
        if (_scheduler != null)
            await _scheduler.Shutdown(_settings.WaitForJobsToComplete).ConfigureAwait(false);
    }

    /// <summary>
    /// Stops faulted.
    /// </summary>
    /// <param name="bus">The bus value.</param>
    /// <param name="exception">The exception associated with the operation.</param>
    /// <returns>The result of the operation.</returns>
    public Task StopFaultedAsync(IBus bus, Exception exception)
    {
        return Task.CompletedTask;
    }
}
