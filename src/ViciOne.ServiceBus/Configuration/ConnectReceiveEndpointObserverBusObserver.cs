using System;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;

namespace ViciOne.ServiceBus.Configuration;

public class ConnectReceiveEndpointObserverBusObserver<T> :
    IBusObserver
    where T : class, IReceiveEndpointObserver
{
    readonly IServiceProvider _provider;

    public ConnectReceiveEndpointObserverBusObserver(IServiceProvider provider)
    {
        _provider = provider;
    }

    public void PostCreate(IBus bus)
    {
        var observer = _provider.GetService<T>();
        if (observer != null)
            bus.ConnectReceiveEndpointObserver(observer);
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
