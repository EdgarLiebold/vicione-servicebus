using System;
using System.Threading;
using System.Threading.Tasks;

namespace ViciOne.ServiceBus.Middleware;

/// <summary>
/// Provides a retry bus observer implementation.
/// </summary>
public class RetryBusObserver :
    IBusObserver,
    IDisposable
{
    readonly object _lock;
    readonly CancellationTokenSource _stopping;
    bool _disposed;

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    public RetryBusObserver()
    {
        _lock = new object();
        _stopping = new CancellationTokenSource();
        Stopping = _stopping.Token;
    }

    /// <summary>
    /// Gets the stopping value.
    /// </summary>
    public CancellationToken Stopping { get; }

    /// <summary>
    /// Performs the post create operation.
    /// </summary>
    /// <param name="bus">The bus value.</param>
    public void PostCreate(IBus bus)
    {
    }

    /// <summary>
    /// Creates faulted.
    /// </summary>
    /// <param name="exception">The exception associated with the operation.</param>
    public void CreateFaulted(Exception exception)
    {
        Dispose();
    }

    /// <summary>
    /// Performs the pre start operation.
    /// </summary>
    /// <param name="bus">The bus value.</param>
    /// <returns>The result of the operation.</returns>
    public Task PreStartAsync(IBus bus)
    {
        return Task.CompletedTask;
    }

    /// <summary>
    /// Performs the post start operation.
    /// </summary>
    /// <param name="bus">The bus value.</param>
    /// <param name="busReady">The bus ready value.</param>
    /// <returns>The result of the operation.</returns>
    public Task PostStartAsync(IBus bus, Task<BusReady> busReady)
    {
        return Task.CompletedTask;
    }

    /// <summary>
    /// Starts faulted.
    /// </summary>
    /// <param name="bus">The bus value.</param>
    /// <param name="exception">The exception associated with the operation.</param>
    /// <returns>The result of the operation.</returns>
    public Task StartFaultedAsync(IBus bus, Exception exception)
    {
        Dispose();

        return Task.CompletedTask;
    }

    /// <summary>
    /// Performs the pre stop operation.
    /// </summary>
    /// <param name="bus">The bus value.</param>
    /// <returns>The result of the operation.</returns>
    public Task PreStopAsync(IBus bus)
    {
        Cancel();

        return Task.CompletedTask;
    }

    /// <summary>
    /// Performs the post stop operation.
    /// </summary>
    /// <param name="bus">The bus value.</param>
    /// <returns>The result of the operation.</returns>
    public Task PostStopAsync(IBus bus)
    {
        Dispose();

        return Task.CompletedTask;
    }

    /// <summary>
    /// Stops faulted.
    /// </summary>
    /// <param name="bus">The bus value.</param>
    /// <param name="exception">The exception associated with the operation.</param>
    /// <returns>The result of the operation.</returns>
    public Task StopFaultedAsync(IBus bus, Exception exception)
    {
        Dispose();

        return Task.CompletedTask;
    }

    /// <summary>
    /// Releases the resources owned by this instance.
    /// </summary>
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
