using System;
using System.Threading.Tasks;
using ViciOne.ServiceBus.Util;

namespace ViciOne.ServiceBus.Observables;

/// <summary>
/// Provides a bus observable implementation.
/// </summary>
public class BusObservable :
    Connectable<IBusObserver>,
    IBusObserver
{
    /// <summary>
    /// Performs the post create operation.
    /// </summary>
    /// <param name="bus">The bus value.</param>
    public void PostCreate(IBus bus)
    {
        ForEach(x => x.PostCreate(bus));
    }

    /// <summary>
    /// Creates faulted.
    /// </summary>
    /// <param name="exception">The exception associated with the operation.</param>
    public void CreateFaulted(Exception exception)
    {
        ForEach(x => x.CreateFaulted(exception));
    }

    /// <summary>
    /// Performs the pre start operation.
    /// </summary>
    /// <param name="bus">The bus value.</param>
    /// <returns>The result of the operation.</returns>
    public Task PreStartAsync(IBus bus)
    {
        return ForEachAsync(x => x.PreStartAsync(bus));
    }

    /// <summary>
    /// Performs the post start operation.
    /// </summary>
    /// <param name="bus">The bus value.</param>
    /// <param name="busReady">The bus ready value.</param>
    /// <returns>The result of the operation.</returns>
    public Task PostStartAsync(IBus bus, Task<BusReady> busReady)
    {
        return ForEachAsync(x => x.PostStartAsync(bus, busReady));
    }

    /// <summary>
    /// Starts faulted.
    /// </summary>
    /// <param name="bus">The bus value.</param>
    /// <param name="exception">The exception associated with the operation.</param>
    /// <returns>The result of the operation.</returns>
    public Task StartFaultedAsync(IBus bus, Exception exception)
    {
        return ForEachAsync(x => x.StartFaultedAsync(bus, exception));
    }

    /// <summary>
    /// Performs the pre stop operation.
    /// </summary>
    /// <param name="bus">The bus value.</param>
    /// <returns>The result of the operation.</returns>
    public Task PreStopAsync(IBus bus)
    {
        return ForEachAsync(x => x.PreStopAsync(bus));
    }

    /// <summary>
    /// Performs the post stop operation.
    /// </summary>
    /// <param name="bus">The bus value.</param>
    /// <returns>The result of the operation.</returns>
    public Task PostStopAsync(IBus bus)
    {
        return ForEachAsync(x => x.PostStopAsync(bus));
    }

    /// <summary>
    /// Stops faulted.
    /// </summary>
    /// <param name="bus">The bus value.</param>
    /// <param name="exception">The exception associated with the operation.</param>
    /// <returns>The result of the operation.</returns>
    public Task StopFaultedAsync(IBus bus, Exception exception)
    {
        return ForEachAsync(x => x.StopFaultedAsync(bus, exception));
    }
}
