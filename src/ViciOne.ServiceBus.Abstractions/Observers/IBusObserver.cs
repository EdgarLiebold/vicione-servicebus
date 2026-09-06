using System;
using System.Threading.Tasks;

namespace ViciOne.ServiceBus.Advanced.Observers;

/// <summary>Used to observe events produced by the bus.</summary>
public interface IBusObserver
{
    /// <summary>Called after the bus has been created.</summary>
    /// <param name="bus">The bus.</param>
    void PostCreate(IBus bus);

    /// <summary>Called when the bus fails to be created.</summary>
    /// <param name="exception">The exception associated with the operation.</param>
    void CreateFaulted(Exception exception);

    /// <summary>Called when the bus is being started, before the actual Start commences.</summary>
    /// <param name="bus">The bus.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    Task PreStartAsync(IBus bus);

    /// <summary>Called once the bus has started and is running.</summary>
    /// <param name="bus">The bus.</param>
    /// <param name="busReady">A task which is completed once the bus is ready and all receive endpoints are ready.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    Task PostStartAsync(IBus bus, Task<BusReady> busReady);

    /// <summary>Called when the bus fails to start.</summary>
    /// <param name="bus">The bus.</param>
    /// <param name="exception">The exception associated with the operation.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    Task StartFaultedAsync(IBus bus, Exception exception);

    /// <summary>Called when the bus is being stopped, before the actual Stop commences.</summary>
    /// <param name="bus">The bus.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    Task PreStopAsync(IBus bus);

    /// <summary>Called when the bus has been stopped.</summary>
    /// <param name="bus">The bus.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    Task PostStopAsync(IBus bus);

    /// <summary>Called when the bus failed to Stop.</summary>
    /// <param name="bus">The bus.</param>
    /// <param name="exception">The exception associated with the operation.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    Task StopFaultedAsync(IBus bus, Exception exception);
}
