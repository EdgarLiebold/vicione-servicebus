using System;
using System.Threading.Tasks;

namespace ViciOne.ServiceBus.JobService;

/// <summary>
/// Provides a job service bus observer implementation.
/// </summary>
public class JobServiceBusObserver :
    IBusObserver
{
    readonly IJobService _jobService;

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="jobService">The job service value.</param>
    public JobServiceBusObserver(IJobService jobService)
    {
        _jobService = jobService;
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

        LogContext.Debug?.Log("Job Service starting: {InstanceAddress}", _jobService.InstanceAddress);

        await _jobService.BusStartedAsync(bus).ConfigureAwait(false);

        LogContext.Info?.Log("Job Service started: {InstanceAddress}", _jobService.InstanceAddress);
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
        LogContext.Debug?.Log("Job Service shutting down: {InstanceAddress}", _jobService.InstanceAddress);

        await _jobService.StopAsync(bus).ConfigureAwait(false);

        LogContext.Info?.Log("Job Service shut down: {InstanceAddress}", _jobService.InstanceAddress);
    }

    /// <summary>
    /// Performs the post stop operation.
    /// </summary>
    /// <param name="bus">The bus value.</param>
    /// <returns>The result of the operation.</returns>
    public Task PostStopAsync(IBus bus)
    {
        return Task.CompletedTask;
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
