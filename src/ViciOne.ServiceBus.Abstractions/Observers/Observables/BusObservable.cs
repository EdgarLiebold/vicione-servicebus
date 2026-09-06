using System;
using System.Threading.Tasks;
using ViciOne.ServiceBus.Util;

namespace ViciOne.ServiceBus.Observables;

/// <summary>Publishes observations for bus.</summary>
public class BusObservable :
    Connectable<IBusObserver>,
    IBusObserver
{
    /// <summary>Runs after create.</summary>
    /// <param name="bus">The bus.</param>
    public void PostCreate(IBus bus)
    {
        ForEach(x => x.PostCreate(bus));
    }

    /// <summary>Creates faulted.</summary>
    /// <param name="exception">The exception associated with the operation.</param>
    public void CreateFaulted(Exception exception)
    {
        ForEach(x => x.CreateFaulted(exception));
    }

    /// <summary>Runs before start.</summary>
    /// <param name="bus">The bus.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public Task PreStartAsync(IBus bus)
    {
        return ForEachAsync(x => x.PreStartAsync(bus));
    }

    /// <summary>Runs after start.</summary>
    /// <param name="bus">The bus.</param>
    /// <param name="busReady">The bus ready.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public Task PostStartAsync(IBus bus, Task<BusReady> busReady)
    {
        return ForEachAsync(x => x.PostStartAsync(bus, busReady));
    }

    /// <summary>Starts faulted.</summary>
    /// <param name="bus">The bus.</param>
    /// <param name="exception">The exception associated with the operation.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public Task StartFaultedAsync(IBus bus, Exception exception)
    {
        return ForEachAsync(x => x.StartFaultedAsync(bus, exception));
    }

    /// <summary>Runs before stop.</summary>
    /// <param name="bus">The bus.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public Task PreStopAsync(IBus bus)
    {
        return ForEachAsync(x => x.PreStopAsync(bus));
    }

    /// <summary>Runs after stop.</summary>
    /// <param name="bus">The bus.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public Task PostStopAsync(IBus bus)
    {
        return ForEachAsync(x => x.PostStopAsync(bus));
    }

    /// <summary>Stops faulted.</summary>
    /// <param name="bus">The bus.</param>
    /// <param name="exception">The exception associated with the operation.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public Task StopFaultedAsync(IBus bus, Exception exception)
    {
        return ForEachAsync(x => x.StopFaultedAsync(bus, exception));
    }
}
