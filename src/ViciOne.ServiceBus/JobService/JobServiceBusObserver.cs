using System;
using System.Threading.Tasks;

namespace ViciOne.ServiceBus.JobService;

/// <summary>Observes job service bus events.</summary>
public class JobServiceBusObserver :
    IBusObserver
{
    readonly IJobService _jobService;

    /// <summary>Initializes a new instance.</summary>
    /// <param name="jobService">The job service.</param>
    public JobServiceBusObserver(IJobService jobService)
    {
        _jobService = jobService;
    }

    /// <summary>Runs after create.</summary>
    /// <param name="bus">The bus.</param>
    public void PostCreate(IBus bus)
    {
    }

    /// <summary>Creates faulted.</summary>
    /// <param name="exception">The exception associated with the operation.</param>
    public void CreateFaulted(Exception exception)
    {
    }

    /// <summary>Runs before start.</summary>
    /// <param name="bus">The bus.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public Task PreStartAsync(IBus bus)
    {
        return Task.CompletedTask;
    }

    /// <summary>Runs after start.</summary>
    /// <param name="bus">The bus.</param>
    /// <param name="busReady">The bus ready.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public async Task PostStartAsync(IBus bus, Task<BusReady> busReady)
    {
        await busReady.ConfigureAwait(false);

        LogContext.Debug?.Log("Job Service starting: {InstanceAddress}", _jobService.InstanceAddress);

        await _jobService.BusStartedAsync(bus).ConfigureAwait(false);

        LogContext.Info?.Log("Job Service started: {InstanceAddress}", _jobService.InstanceAddress);
    }

    /// <summary>Starts faulted.</summary>
    /// <param name="bus">The bus.</param>
    /// <param name="exception">The exception associated with the operation.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public Task StartFaultedAsync(IBus bus, Exception exception)
    {
        return Task.CompletedTask;
    }

    /// <summary>Runs before stop.</summary>
    /// <param name="bus">The bus.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public async Task PreStopAsync(IBus bus)
    {
        LogContext.Debug?.Log("Job Service shutting down: {InstanceAddress}", _jobService.InstanceAddress);

        await _jobService.StopAsync(bus).ConfigureAwait(false);

        LogContext.Info?.Log("Job Service shut down: {InstanceAddress}", _jobService.InstanceAddress);
    }

    /// <summary>Runs after stop.</summary>
    /// <param name="bus">The bus.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public Task PostStopAsync(IBus bus)
    {
        return Task.CompletedTask;
    }

    /// <summary>Stops faulted.</summary>
    /// <param name="bus">The bus.</param>
    /// <param name="exception">The exception associated with the operation.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public Task StopFaultedAsync(IBus bus, Exception exception)
    {
        return Task.CompletedTask;
    }
}
