using System;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;

namespace ViciOne.ServiceBus.Configuration;

/// <summary>Observes connect receive endpoint observer bus events.</summary>
/// <typeparam name="T">The value type.</typeparam>
public class ConnectReceiveEndpointObserverBusObserver<T> :
    IBusObserver
    where T : class, IReceiveEndpointObserver
{
    readonly IServiceProvider _provider;

    /// <summary>Initializes a new instance.</summary>
    /// <param name="provider">The service provider used to resolve dependencies.</param>
    public ConnectReceiveEndpointObserverBusObserver(IServiceProvider provider)
    {
        _provider = provider;
    }

    /// <summary>Runs after create.</summary>
    /// <param name="bus">The bus.</param>
    public void PostCreate(IBus bus)
    {
        var observer = _provider.GetService<T>();
        if (observer != null)
            bus.ConnectReceiveEndpointObserver(observer);
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
