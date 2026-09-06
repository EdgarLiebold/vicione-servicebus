using System;
using System.Threading.Tasks;

namespace ViciOne.ServiceBus.JobService;

/// <summary>Starts and stops the local job runtime with its owning bus.</summary>
internal sealed class JobServiceBusObserver :
    IBusObserver
{
    readonly IJobService _jobService;

    /// <summary>Initializes a new instance.</summary>
    /// <param name="jobService">The job service.</param>
    public JobServiceBusObserver(IJobService jobService)
    {
        _jobService = jobService ?? throw new ArgumentNullException(nameof(jobService));
    }

    /// <summary>Requires no action after bus creation.</summary>
    /// <param name="bus">The bus.</param>
    public void PostCreate(IBus bus) => ArgumentNullException.ThrowIfNull(bus);

    /// <summary>Requires no cleanup when bus creation fails.</summary>
    /// <param name="exception">The exception associated with the operation.</param>
    public void CreateFaulted(Exception exception) => ArgumentNullException.ThrowIfNull(exception);

    /// <summary>Requires no action before bus startup.</summary>
    /// <param name="bus">The bus.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public Task PreStartAsync(IBus bus) => bus is null
        ? Task.FromException(new ArgumentNullException(nameof(bus)))
        : Task.CompletedTask;

    /// <summary>Runs after start.</summary>
    /// <param name="bus">The bus.</param>
    /// <param name="busReady">The bus ready.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public async Task PostStartAsync(IBus bus, Task<BusReady> busReady)
    {
        ArgumentNullException.ThrowIfNull(bus);
        ArgumentNullException.ThrowIfNull(busReady);
        await busReady.ConfigureAwait(false);

        LogContext.Debug?.Log("Job Service starting: {InstanceAddress}", _jobService.InstanceAddress);

        await _jobService.BusStartedAsync(bus).ConfigureAwait(false);

        LogContext.Info?.Log("Job Service started: {InstanceAddress}", _jobService.InstanceAddress);
    }

    /// <summary>Requires no additional cleanup when bus startup fails.</summary>
    /// <param name="bus">The bus.</param>
    /// <param name="exception">The exception associated with the operation.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public Task StartFaultedAsync(IBus bus, Exception exception) => bus is null
        ? Task.FromException(new ArgumentNullException(nameof(bus)))
        : exception is null
            ? Task.FromException(new ArgumentNullException(nameof(exception)))
            : Task.CompletedTask;

    /// <summary>Runs before stop.</summary>
    /// <param name="bus">The bus.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public async Task PreStopAsync(IBus bus)
    {
        ArgumentNullException.ThrowIfNull(bus);
        LogContext.Debug?.Log("Job Service shutting down: {InstanceAddress}", _jobService.InstanceAddress);

        await _jobService.StopAsync(bus).ConfigureAwait(false);

        LogContext.Info?.Log("Job Service shut down: {InstanceAddress}", _jobService.InstanceAddress);
    }

    /// <summary>Requires no action after the bus has stopped.</summary>
    /// <param name="bus">The bus.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public Task PostStopAsync(IBus bus) => bus is null
        ? Task.FromException(new ArgumentNullException(nameof(bus)))
        : Task.CompletedTask;

    /// <summary>Requires no additional cleanup when bus shutdown fails.</summary>
    /// <param name="bus">The bus.</param>
    /// <param name="exception">The exception associated with the operation.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public Task StopFaultedAsync(IBus bus, Exception exception) => bus is null
        ? Task.FromException(new ArgumentNullException(nameof(bus)))
        : exception is null
            ? Task.FromException(new ArgumentNullException(nameof(exception)))
            : Task.CompletedTask;
}
