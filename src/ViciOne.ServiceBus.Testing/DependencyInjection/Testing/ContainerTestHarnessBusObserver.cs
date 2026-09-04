using System;
using System.Threading.Tasks;

namespace ViciOne.ServiceBus.DependencyInjection.Testing;

public class ContainerTestHarnessBusObserver :
    IBusObserver
{
    readonly ContainerTestHarness _harness;

    public ContainerTestHarnessBusObserver(ContainerTestHarness harness)
    {
        _harness = harness;
    }

    public void PostCreate(IBus bus)
    {
        _harness.PostCreate(bus);
    }

    public void CreateFaulted(Exception exception)
    {
    }

    public Task PreStartAsync(IBus bus)
    {
        return Task.CompletedTask;
    }

    public Task PostStartAsync(IBus bus, Task<BusReady> busReady)
    {
        _harness.PostStart(bus);

        return Task.CompletedTask;
    }

    public Task StartFaultedAsync(IBus bus, Exception exception)
    {
        return Task.CompletedTask;
    }

    public Task PreStopAsync(IBus bus)
    {
        return Task.CompletedTask;
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
