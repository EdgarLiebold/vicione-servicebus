using System;
using System.Threading.Tasks;
using ViciOne.ServiceBus.Util;

namespace ViciOne.ServiceBus.Observables;

/// <summary>Fans out bus lifecycle notifications to a stable snapshot of connected observers.</summary>
public class BusObservable :
    Connectable<IBusObserver>,
    IBusObserver
{
    /// <summary>Notifies observers after a bus has been constructed.</summary>
    /// <param name="bus">The constructed bus.</param>
    public void PostCreate(IBus bus)
    {
        ArgumentNullException.ThrowIfNull(bus);
        ForEach(x => x.PostCreate(bus));
    }

    /// <summary>Notifies observers that bus construction failed.</summary>
    /// <param name="exception">The construction failure.</param>
    public void CreateFaulted(Exception exception)
    {
        ArgumentNullException.ThrowIfNull(exception);
        ForEach(x => x.CreateFaulted(exception));
    }

    /// <summary>Notifies observers before bus startup begins.</summary>
    /// <param name="bus">The bus that will start.</param>
    /// <returns>A task that completes after every observer has processed the notification.</returns>
    public Task PreStartAsync(IBus bus)
    {
        ArgumentNullException.ThrowIfNull(bus);
        return ForEachAsync(x => x.PreStartAsync(bus));
    }

    /// <summary>Notifies observers after startup has produced its readiness task.</summary>
    /// <param name="bus">The started bus.</param>
    /// <param name="busReady">The task that completes when the bus and all receive endpoints are ready.</param>
    /// <returns>A task that completes after every observer has processed the notification.</returns>
    public Task PostStartAsync(IBus bus, Task<BusReady> busReady)
    {
        ArgumentNullException.ThrowIfNull(bus);
        ArgumentNullException.ThrowIfNull(busReady);
        return ForEachAsync(x => x.PostStartAsync(bus, busReady));
    }

    /// <summary>Notifies observers that bus startup failed.</summary>
    /// <param name="bus">The bus whose startup failed.</param>
    /// <param name="exception">The startup failure.</param>
    /// <returns>A task that completes after every observer has processed the notification.</returns>
    public Task StartFaultedAsync(IBus bus, Exception exception)
    {
        ArgumentNullException.ThrowIfNull(bus);
        ArgumentNullException.ThrowIfNull(exception);
        return ForEachAsync(x => x.StartFaultedAsync(bus, exception));
    }

    /// <summary>Notifies observers before bus shutdown begins.</summary>
    /// <param name="bus">The bus that will stop.</param>
    /// <returns>A task that completes after every observer has processed the notification.</returns>
    public Task PreStopAsync(IBus bus)
    {
        ArgumentNullException.ThrowIfNull(bus);
        return ForEachAsync(x => x.PreStopAsync(bus));
    }

    /// <summary>Notifies observers after the bus has stopped.</summary>
    /// <param name="bus">The stopped bus.</param>
    /// <returns>A task that completes after every observer has processed the notification.</returns>
    public Task PostStopAsync(IBus bus)
    {
        ArgumentNullException.ThrowIfNull(bus);
        return ForEachAsync(x => x.PostStopAsync(bus));
    }

    /// <summary>Notifies observers that bus shutdown failed.</summary>
    /// <param name="bus">The bus whose shutdown failed.</param>
    /// <param name="exception">The shutdown failure.</param>
    /// <returns>A task that completes after every observer has processed the notification.</returns>
    public Task StopFaultedAsync(IBus bus, Exception exception)
    {
        ArgumentNullException.ThrowIfNull(bus);
        ArgumentNullException.ThrowIfNull(exception);
        return ForEachAsync(x => x.StopFaultedAsync(bus, exception));
    }
}
