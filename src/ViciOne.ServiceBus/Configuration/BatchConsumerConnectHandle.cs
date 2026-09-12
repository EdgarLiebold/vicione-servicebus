using System;
using System.Runtime.ExceptionServices;
using System.Threading;
using System.Threading.Tasks;

namespace ViciOne.ServiceBus.Configuration;

/// <summary>Disconnects a batch message pipe and drains its collector as one idempotent operation.</summary>
internal sealed class BatchConsumerConnectHandle :
    ConnectHandle
{
    readonly IAsyncDisposable _batchLifetime;
    readonly ConnectHandle _handle;
    readonly object _lock = new();
    Task? _disposeTask;
    int _observationStarted;

    /// <summary>Creates a handle over the connected message pipe and its batch lifetime.</summary>
    /// <param name="handle">The message-pipe connection to disconnect.</param>
    /// <param name="batchLifetime">The collector lifetime to drain after disconnection.</param>
    public BatchConsumerConnectHandle(ConnectHandle handle, IAsyncDisposable batchLifetime)
    {
        _handle = handle ?? throw new ArgumentNullException(nameof(handle));
        _batchLifetime = batchLifetime ?? throw new ArgumentNullException(nameof(batchLifetime));
    }

    /// <summary>Starts disconnection and observes any asynchronous cleanup failure.</summary>
    public void Dispose() => Disconnect();

    /// <summary>Stops new message admission and begins draining accepted batches.</summary>
    public void Disconnect()
    {
        Task cleanup = BeginDisconnectAsync();
        if (Interlocked.Exchange(ref _observationStarted, 1) == 0)
            _ = ObserveCleanupFailureAsync(cleanup);
    }

    /// <summary>Disconnects the message pipe and awaits batch-lifetime cleanup.</summary>
    /// <returns>A value task that completes after both owned resources have terminated.</returns>
    public ValueTask DisposeAsync() => new(BeginDisconnectAsync());

    Task BeginDisconnectAsync()
    {
        lock (_lock)
        {
            if (_disposeTask != null)
                return _disposeTask;

            Exception? disconnectFailure = null;
            try
            {
                _handle.Disconnect();
            }
            catch (Exception exception)
            {
                disconnectFailure = exception;
            }

            Task batchCleanup;
            try
            {
                batchCleanup = _batchLifetime.DisposeAsync().AsTask();
            }
            catch (Exception exception)
            {
                batchCleanup = Task.FromException(exception);
            }

            _disposeTask = CompleteDisconnectAsync(disconnectFailure, batchCleanup);
            return _disposeTask;
        }
    }

    static async Task CompleteDisconnectAsync(Exception? disconnectFailure, Task batchCleanup)
    {
        Exception? cleanupFailure = null;
        try
        {
            await batchCleanup.ConfigureAwait(false);
        }
        catch (Exception exception)
        {
            cleanupFailure = exception;
        }

        if (disconnectFailure != null && cleanupFailure != null)
            throw new AggregateException("Batch consumer disconnection encountered multiple failures.", disconnectFailure, cleanupFailure);
        if (disconnectFailure != null)
            ExceptionDispatchInfo.Capture(disconnectFailure).Throw();
        if (cleanupFailure != null)
            ExceptionDispatchInfo.Capture(cleanupFailure).Throw();
    }

    static async Task ObserveCleanupFailureAsync(Task cleanup)
    {
        try
        {
            await cleanup.ConfigureAwait(false);
        }
        catch (Exception exception)
        {
            LogContext.Error?.Log(exception, "Batch consumer lifetime disposal faulted");
        }
    }
}
