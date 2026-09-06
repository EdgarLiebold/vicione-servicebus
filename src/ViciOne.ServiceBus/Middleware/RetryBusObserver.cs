using System;
using System.Threading;
using System.Threading.Tasks;

namespace ViciOne.ServiceBus.Middleware;

/// <summary>Observes retry bus events.</summary>
public class RetryBusObserver :
    IBusObserver,
    IDisposable
{
    readonly object _lock;
    readonly CancellationTokenSource _stopping;
    bool _disposed;

    /// <summary>Initializes a new instance.</summary>
    public RetryBusObserver()
    {
        _lock = new object();
        _stopping = new CancellationTokenSource();
        Stopping = _stopping.Token;
    }

    /// <summary>Gets the stopping.</summary>
    public CancellationToken Stopping { get; }

    /// <summary>Runs after create.</summary>
    /// <param name="bus">The bus.</param>
    public void PostCreate(IBus bus)
    {
    }

    /// <summary>Creates faulted.</summary>
    /// <param name="exception">The exception associated with the operation.</param>
    public void CreateFaulted(Exception exception)
    {
        Dispose();
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
        Dispose();

        return Task.CompletedTask;
    }

    /// <summary>Runs before stop.</summary>
    /// <param name="bus">The bus.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public Task PreStopAsync(IBus bus)
    {
        Cancel();

        return Task.CompletedTask;
    }

    /// <summary>Runs after stop.</summary>
    /// <param name="bus">The bus.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public Task PostStopAsync(IBus bus)
    {
        Dispose();

        return Task.CompletedTask;
    }

    /// <summary>Stops faulted.</summary>
    /// <param name="bus">The bus.</param>
    /// <param name="exception">The exception associated with the operation.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public Task StopFaultedAsync(IBus bus, Exception exception)
    {
        Dispose();

        return Task.CompletedTask;
    }

    /// <summary>Releases the resources owned by this instance.</summary>
    public void Dispose()
    {
        lock (_lock)
        {
            if (_disposed)
                return;

            if (!_stopping.IsCancellationRequested)
                _stopping.Cancel();

            _stopping.Dispose();
            _disposed = true;
        }
    }

    void Cancel()
    {
        lock (_lock)
        {
            if (!_disposed && !_stopping.IsCancellationRequested)
                _stopping.Cancel();
        }
    }

}
