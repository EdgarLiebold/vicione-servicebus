using System;
using System.Threading.Tasks;

namespace ViciOne.ServiceBus.JobService;

/// <summary>Starts and stops the local job runtime with its owning bus.</summary>
/// <param name="jobService">The runtime to start and stop with the bus.</param>
internal sealed class JobServiceBusObserver(IJobService jobService) :
    IBusObserver
{
    readonly IJobService _jobService = jobService ?? throw new ArgumentNullException(nameof(jobService));

    /// <summary>Requires no action after bus creation.</summary>
    /// <param name="bus">The created bus.</param>
    public void PostCreate(IBus bus) => ArgumentNullException.ThrowIfNull(bus);

    /// <summary>Requires no cleanup when bus creation fails.</summary>
    /// <param name="exception">The bus-creation failure.</param>
    public void CreateFaulted(Exception exception) => ArgumentNullException.ThrowIfNull(exception);

    /// <summary>Requires no action before bus startup.</summary>
    /// <param name="bus">The bus about to start.</param>
    /// <returns>A completed task after argument validation.</returns>
    public Task PreStartAsync(IBus bus) => bus is null
        ? Task.FromException(new ArgumentNullException(nameof(bus)))
        : Task.CompletedTask;

    /// <summary>Starts the local job runtime after all bus endpoints report readiness.</summary>
    /// <param name="bus">The running bus used to publish job-service availability.</param>
    /// <param name="busReady">The readiness task for the bus endpoints.</param>
    /// <returns>A task that completes after the runtime has announced every registered job type.</returns>
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
    /// <param name="bus">The bus whose startup failed.</param>
    /// <param name="exception">The startup failure.</param>
    /// <returns>A completed task after argument validation.</returns>
    public Task StartFaultedAsync(IBus bus, Exception exception) => bus is null
        ? Task.FromException(new ArgumentNullException(nameof(bus)))
        : exception is null
            ? Task.FromException(new ArgumentNullException(nameof(exception)))
            : Task.CompletedTask;

    /// <summary>Stops job admission, heartbeats, and local executions before the bus stops.</summary>
    /// <param name="bus">The bus used to publish the instance-stopped notifications.</param>
    /// <returns>A task that completes after the local job runtime has drained.</returns>
    public async Task PreStopAsync(IBus bus)
    {
        ArgumentNullException.ThrowIfNull(bus);
        LogContext.Debug?.Log("Job Service shutting down: {InstanceAddress}", _jobService.InstanceAddress);

        await _jobService.StopAsync(bus).ConfigureAwait(false);

        LogContext.Info?.Log("Job Service shut down: {InstanceAddress}", _jobService.InstanceAddress);
    }

    /// <summary>Requires no action after the bus has stopped.</summary>
    /// <param name="bus">The stopped bus.</param>
    /// <returns>A completed task after argument validation.</returns>
    public Task PostStopAsync(IBus bus) => bus is null
        ? Task.FromException(new ArgumentNullException(nameof(bus)))
        : Task.CompletedTask;

    /// <summary>Requires no additional cleanup when bus shutdown fails.</summary>
    /// <param name="bus">The bus whose shutdown failed.</param>
    /// <param name="exception">The shutdown failure.</param>
    /// <returns>A completed task after argument validation.</returns>
    public Task StopFaultedAsync(IBus bus, Exception exception) => bus is null
        ? Task.FromException(new ArgumentNullException(nameof(bus)))
        : exception is null
            ? Task.FromException(new ArgumentNullException(nameof(exception)))
            : Task.CompletedTask;
}
