using System;
using System.Threading.Tasks;

namespace ViciOne.ServiceBus.Advanced.Observers;

/// <summary>Observes construction, startup, and shutdown events for a bus.</summary>
public interface IBusObserver
{
    /// <summary>Called after the bus has been constructed.</summary>
    /// <param name="bus">The constructed bus.</param>
    void PostCreate(IBus bus);

    /// <summary>Called when bus construction fails.</summary>
    /// <param name="exception">The construction failure.</param>
    void CreateFaulted(Exception exception);

    /// <summary>Called before bus startup begins.</summary>
    /// <param name="bus">The bus that will start.</param>
    /// <returns>An awaitable barrier that delays transport startup until the observer is ready.</returns>
    Task PreStartAsync(IBus bus);

    /// <summary>Called after startup has produced the bus readiness task.</summary>
    /// <param name="bus">The started bus.</param>
    /// <param name="busReady">The task that completes when the bus and all receive endpoints are ready.</param>
    /// <returns>An awaitable barrier for processing the bus readiness result.</returns>
    Task PostStartAsync(IBus bus, Task<BusReady> busReady);

    /// <summary>Called when bus startup fails.</summary>
    /// <param name="bus">The bus whose startup failed.</param>
    /// <param name="exception">The startup failure.</param>
    /// <returns>An awaitable result for processing the startup failure.</returns>
    Task StartFaultedAsync(IBus bus, Exception exception);

    /// <summary>Called before bus shutdown begins.</summary>
    /// <param name="bus">The bus that will stop.</param>
    /// <returns>An awaitable barrier that delays transport shutdown until the observer is ready.</returns>
    Task PreStopAsync(IBus bus);

    /// <summary>Called after the bus has stopped.</summary>
    /// <param name="bus">The stopped bus.</param>
    /// <returns>An awaitable result for processing the completed shutdown.</returns>
    Task PostStopAsync(IBus bus);

    /// <summary>Called when bus shutdown fails.</summary>
    /// <param name="bus">The bus whose shutdown failed.</param>
    /// <param name="exception">The shutdown failure.</param>
    /// <returns>An awaitable result for processing the shutdown failure.</returns>
    Task StopFaultedAsync(IBus bus, Exception exception);
}
