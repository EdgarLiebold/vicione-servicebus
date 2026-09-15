using System;
using System.Threading;
using System.Threading.Tasks;

namespace ViciOne.ServiceBus.Middleware;

/// <summary>Cancels bus-scoped retry delays when the bus stops or cannot start.</summary>
internal sealed class RetryBusObserver :
    IBusObserver,
    IDisposable
{
    readonly object _lock;
    readonly CancellationTokenSource _stopping;
    bool _disposed;

    /// <summary>Creates an observer with an active stopping token.</summary>
    public RetryBusObserver()
    {
        _lock = new object();
        _stopping = new CancellationTokenSource();
        Stopping = _stopping.Token;
    }

    /// <summary>Gets the token canceled when retry processing must stop.</summary>
    public CancellationToken Stopping { get; }

    /// <summary>Leaves retry processing active after bus creation.</summary>
    /// <param name="bus">The created bus.</param>
    public void PostCreate(IBus bus)
    {
    }

    /// <summary>Cancels retry processing when bus creation fails.</summary>
    /// <param name="exception">The bus-creation failure.</param>
    public void CreateFaulted(Exception exception)
    {
        Dispose();
    }

    /// <summary>Leaves retry processing active before the bus starts.</summary>
    /// <param name="bus">The bus being started.</param>
    /// <returns>A completed task.</returns>
    public Task PreStartAsync(IBus bus)
    {
        return Task.CompletedTask;
    }

    /// <summary>Leaves retry processing active after the bus starts.</summary>
    /// <param name="bus">The started bus.</param>
    /// <param name="busReady">The task that reports bus readiness.</param>
    /// <returns>A completed task.</returns>
    public Task PostStartAsync(IBus bus, Task<BusReady> busReady)
    {
        return Task.CompletedTask;
    }

    /// <summary>Cancels retry processing when bus startup fails.</summary>
    /// <param name="bus">The bus that failed to start.</param>
    /// <param name="exception">The startup failure.</param>
    /// <returns>A completed task.</returns>
    public Task StartFaultedAsync(IBus bus, Exception exception)
    {
        Dispose();

        return Task.CompletedTask;
    }

    /// <summary>Cancels pending retry delays before the bus stops.</summary>
    /// <param name="bus">The bus being stopped.</param>
    /// <returns>A completed task.</returns>
    public Task PreStopAsync(IBus bus)
    {
        Cancel();

        return Task.CompletedTask;
    }

    /// <summary>Releases retry-cancellation resources after the bus stops.</summary>
    /// <param name="bus">The stopped bus.</param>
    /// <returns>A completed task.</returns>
    public Task PostStopAsync(IBus bus)
    {
        Dispose();

        return Task.CompletedTask;
    }

    /// <summary>Releases retry-cancellation resources when bus shutdown fails.</summary>
    /// <param name="bus">The bus that failed to stop.</param>
    /// <param name="exception">The shutdown failure.</param>
    /// <returns>A completed task.</returns>
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
