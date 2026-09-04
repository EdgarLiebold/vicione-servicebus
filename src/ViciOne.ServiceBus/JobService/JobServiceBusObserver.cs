using System;
using System.Threading.Tasks;

namespace ViciOne.ServiceBus.JobService;

public class JobServiceBusObserver :
    IBusObserver
{
    readonly IJobService _jobService;

    public JobServiceBusObserver(IJobService jobService)
    {
        _jobService = jobService;
    }

    public void PostCreate(IBus bus)
    {
    }

    public void CreateFaulted(Exception exception)
    {
    }

    public Task PreStartAsync(IBus bus)
    {
        return Task.CompletedTask;
    }

    public async Task PostStartAsync(IBus bus, Task<BusReady> busReady)
    {
        await busReady.ConfigureAwait(false);

        LogContext.Debug?.Log("Job Service starting: {InstanceAddress}", _jobService.InstanceAddress);

        await _jobService.BusStartedAsync(bus).ConfigureAwait(false);

        LogContext.Info?.Log("Job Service started: {InstanceAddress}", _jobService.InstanceAddress);
    }

    public Task StartFaultedAsync(IBus bus, Exception exception)
    {
        return Task.CompletedTask;
    }

    public async Task PreStopAsync(IBus bus)
    {
        LogContext.Debug?.Log("Job Service shutting down: {InstanceAddress}", _jobService.InstanceAddress);

        await _jobService.StopAsync(bus).ConfigureAwait(false);

        LogContext.Info?.Log("Job Service shut down: {InstanceAddress}", _jobService.InstanceAddress);
    }

    public Task PostStopAsync(IBus bus)
    {
        return Task.CompletedTask;
    }

    public Task StopFaultedAsync(IBus bus, Exception exception)
    {
        return Task.CompletedTask;
    }
}
