using System;
using System.Threading.Tasks;

namespace ViciOne.ServiceBus.DependencyInjection.Testing;

/// <summary>Observes container test harness bus events.</summary>
public class ContainerTestHarnessBusObserver :
    IBusObserver
{
    readonly ContainerTestHarness _harness;

    /// <summary>Initializes a new instance.</summary>
    /// <param name="harness">The harness.</param>
    public ContainerTestHarnessBusObserver(ContainerTestHarness harness)
    {
        _harness = harness;
    }

    /// <summary>Runs after create.</summary>
    /// <param name="bus">The bus.</param>
    public void PostCreate(IBus bus)
    {
        _harness.PostCreate(bus);
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
    public Task PostStartAsync(IBus bus, Task<BusReady> busReady)
    {
        _harness.PostStart(bus);

        return Task.CompletedTask;
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
    public Task PreStopAsync(IBus bus)
    {
        return Task.CompletedTask;
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
