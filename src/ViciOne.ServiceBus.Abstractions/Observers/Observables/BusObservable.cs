using System;
using System.Threading.Tasks;
using ViciOne.ServiceBus.Util;

namespace ViciOne.ServiceBus.Observables;

public class BusObservable :
    Connectable<IBusObserver>,
    IBusObserver
{
    public void PostCreate(IBus bus)
    {
        ForEach(x => x.PostCreate(bus));
    }

    public void CreateFaulted(Exception exception)
    {
        ForEach(x => x.CreateFaulted(exception));
    }

    public Task PreStartAsync(IBus bus)
    {
        return ForEachAsync(x => x.PreStartAsync(bus));
    }

    public Task PostStartAsync(IBus bus, Task<BusReady> busReady)
    {
        return ForEachAsync(x => x.PostStartAsync(bus, busReady));
    }

    public Task StartFaultedAsync(IBus bus, Exception exception)
    {
        return ForEachAsync(x => x.StartFaultedAsync(bus, exception));
    }

    public Task PreStopAsync(IBus bus)
    {
        return ForEachAsync(x => x.PreStopAsync(bus));
    }

    public Task PostStopAsync(IBus bus)
    {
        return ForEachAsync(x => x.PostStopAsync(bus));
    }

    public Task StopFaultedAsync(IBus bus, Exception exception)
    {
        return ForEachAsync(x => x.StopFaultedAsync(bus, exception));
    }
}
