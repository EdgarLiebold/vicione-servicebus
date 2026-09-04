using System;
using System.Threading;
using System.Threading.Tasks;

namespace ViciOne.ServiceBus.Middleware;

public class RetryBusObserver :
    IBusObserver,
    IDisposable
{
    readonly object _lock;
    readonly CancellationTokenSource _stopping;
    bool _disposed;

    public RetryBusObserver()
    {
        _lock = new object();
        _stopping = new CancellationTokenSource();
        Stopping = _stopping.Token;
    }

    public CancellationToken Stopping { get; }

    public void PostCreate(IBus bus)
    {
    }

    public void CreateFaulted(Exception exception)
    {
        Dispose();
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
        Dispose();

        return Task.CompletedTask;
    }

    public Task PreStopAsync(IBus bus)
    {
        Cancel();

        return Task.CompletedTask;
    }

    public Task PostStopAsync(IBus bus)
    {
        Dispose();

        return Task.CompletedTask;
    }

    public Task StopFaultedAsync(IBus bus, Exception exception)
    {
        Dispose();

        return Task.CompletedTask;
    }

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
