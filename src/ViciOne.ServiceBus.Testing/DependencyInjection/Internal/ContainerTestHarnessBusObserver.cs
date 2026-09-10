using System;
using System.Threading.Tasks;

namespace ViciOne.ServiceBus.Testing.Internal;

/// <summary>Bridges bus lifecycle notifications into the dependency-injection test harness.</summary>
internal sealed class ContainerTestHarnessBusObserver :
    IBusObserver
{
    readonly ContainerTestHarness _harness;

    /// <summary>Creates a lifecycle bridge for a container-owned harness.</summary>
    /// <param name="harness">The harness that receives bus lifecycle notifications.</param>
    public ContainerTestHarnessBusObserver(ContainerTestHarness harness)
    {
        _harness = harness ?? throw new ArgumentNullException(nameof(harness));
    }

    /// <inheritdoc />
    public void PostCreate(IBus bus)
    {
        _harness.PostCreate(bus);
    }

    /// <inheritdoc />
    public void CreateFaulted(Exception exception)
    {
    }

    /// <inheritdoc />
    public Task PreStartAsync(IBus bus)
    {
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task PostStartAsync(IBus bus, Task<BusReady> busReady)
    {
        return _harness.PostStartAsync();
    }

    /// <inheritdoc />
    public Task StartFaultedAsync(IBus bus, Exception exception)
    {
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task PreStopAsync(IBus bus)
    {
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task PostStopAsync(IBus bus)
    {
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task StopFaultedAsync(IBus bus, Exception exception)
    {
        return Task.CompletedTask;
    }
}
