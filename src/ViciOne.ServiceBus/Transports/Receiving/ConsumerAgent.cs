using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using ViciOne.ServiceBus.Internals;
using ViciOne.ServiceBus.Middleware;
using ViciOne.ServiceBus.Util;

namespace ViciOne.ServiceBus.Transports;

/// <summary>Coordinates a transport consume loop, keyed duplicate deliveries, and graceful shutdown.</summary>
/// <typeparam name="TKey">The transport-specific identity used to recognize duplicate deliveries.</typeparam>
public abstract class ConsumerAgent<TKey> :
    Agent,
    IDeliveryMetrics
    where TKey : notnull
{
    readonly ReceiveEndpointContext _context;
    readonly TaskCompletionSource<bool> _deliveryComplete;
    readonly IReceivePipeDispatcher _dispatcher;
    readonly object _consumeTaskLock = new();
    readonly ConcurrentDictionary<TKey, PendingReceiveLockContext> _pending;
    readonly object _pendingLock = new();
    Task? _consumeTask;
    Task? _consumeTaskObserver;
    TaskCompletionSource<bool>? _consumeTaskSource;
    int _gracefulShutdown = 1;

    /// <summary>Initializes a consumer agent for one receive endpoint.</summary>
    /// <param name="context">The endpoint context that owns the consume pipeline and lifecycle.</param>
    /// <param name="equalityComparer">The comparer used for transport delivery identities.</param>
    protected ConsumerAgent(ReceiveEndpointContext context, IEqualityComparer<TKey>? equalityComparer = default)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _deliveryComplete = TaskCompletionSources.Create<bool>();

        _pending = new ConcurrentDictionary<TKey, PendingReceiveLockContext>(equalityComparer ?? EqualityComparer<TKey>.Default);

        _dispatcher = context.CreateReceivePipeDispatcher()
            ?? throw new InvalidOperationException("The receive endpoint context returned no pipe dispatcher.");
        _dispatcher.ZeroActivity += HandleDeliveryCompleteAsync;
    }

    /// <summary>Gets whether no delivery is currently being dispatched.</summary>
    protected bool IsIdle => ActiveDispatchCount == 0;

    /// <summary>Gets the number of deliveries currently being dispatched.</summary>
    protected long ActiveDispatchCount => _dispatcher.ActiveDispatchCount;

    /// <summary>Gets whether the consume loop remained active until shutdown was requested.</summary>
    protected bool IsGracefulShutdown => Volatile.Read(ref _gracefulShutdown) != 0;

    /// <summary>Gets the total number of deliveries dispatched by this agent.</summary>
    public long DeliveryCount => _dispatcher.DispatchCount;

    /// <summary>Gets the highest number of deliveries observed concurrently.</summary>
    public int MaxConcurrentDeliveryCount => _dispatcher.MaxConcurrentDispatchCount;

    Task HandleDeliveryCompleteAsync()
    {
        if (IsStopping)
            _deliveryComplete.TrySetResult(true);

        return Task.CompletedTask;
    }

    /// <summary>Creates a manually completed task to represent a callback-based consume loop.</summary>
    protected void TrySetManualConsumeTask()
    {
        lock (_consumeTaskLock)
        {
            if (_consumeTask is not null || _consumeTaskSource is not null)
                return;

            _consumeTaskSource = TaskCompletionSources.Create<bool>();
            _consumeTask = _consumeTaskSource.Task;
            SetConsumeTask(_consumeTask);
        }
    }

    /// <summary>Registers the task that represents the transport consume loop.</summary>
    /// <param name="consumeTask">The task that completes when the consume loop exits.</param>
    protected void TrySetConsumeTask(Task consumeTask)
    {
        ArgumentNullException.ThrowIfNull(consumeTask);

        lock (_consumeTaskLock)
        {
            if (_consumeTask is not null)
                return;

            _consumeTask = consumeTask;
            SetConsumeTask(_consumeTask);
        }
    }

    void SetConsumeTask(Task consumeTask)
    {
        _consumeTaskObserver = ObserveConsumeTaskAsync(consumeTask);
    }

    async Task ObserveConsumeTaskAsync(Task consumeTask)
    {
        try
        {
            await consumeTask.ConfigureAwait(false);
        }
        catch
        {
            // Shutdown consumes and logs the transport-specific failure. This observer owns the
            // automatic lifecycle transition when the loop terminates unexpectedly.
        }

        if (IsStopping)
            return;

        Volatile.Write(ref _gracefulShutdown, 0);

        try
        {
            LogContext.SetCurrentIfNull(_context.LogContext);

            using var tokenSource = _context.StopTimeout.HasValue
                ? new CancellationTokenSource(_context.StopTimeout.Value, _context.GetTimeProvider())
                : new CancellationTokenSource();

            await this.StopAsync("Consume Loop Exited", tokenSource.Token).ConfigureAwait(false);
        }
        catch (Exception exception)
        {
            LogContext.Warning?.Log(exception, "Stop Faulted");
        }
    }

    /// <summary>Stops accepting deliveries and waits for dispatches and the consume loop to complete.</summary>
    /// <param name="context">The reason and cancellation token for shutdown.</param>
    /// <returns>The task that represents completion of the consumer agent.</returns>
    protected override Task StopAgentAsync(StopContext context)
    {
        try
        {
            LogContext.Debug?.Log("Consumer Stopping: {InputAddress} ({Reason})", _context.InputAddress, context.Reason);
        }
        catch (Exception)
        {
        }

        TrySetConsumeCompleted();

        SetCompleted(ActiveAndActualAgentsCompletedAsync(context));

        return Completed;
    }

    void CancelPendingConsumers()
    {
        PendingReceiveLockContext[] pending;
        lock (_pendingLock)
        {
            pending = new List<PendingReceiveLockContext>(_pending.Values).ToArray();
            _pending.Clear();
        }

        List<Exception>? failures = null;
        foreach (PendingReceiveLockContext context in pending)
        {
            try
            {
                context.Cancel();
            }
            catch (Exception exception)
            {
                (failures ??= new List<Exception>()).Add(exception);
            }
        }

        if (failures != null)
            throw new AggregateException("One or more retained deliveries failed during cancellation.", failures);
    }

    /// <summary>Completes a manually managed consume loop successfully.</summary>
    protected void TrySetConsumeCompleted()
    {
        _consumeTaskSource?.TrySetResult(true);
    }

    /// <summary>Cancels a manually managed consume loop and all retained duplicate deliveries.</summary>
    /// <param name="cancellationToken">The token recorded on the canceled consume task.</param>
    protected void TrySetConsumeCanceled(CancellationToken cancellationToken = default)
    {
        if (_consumeTaskSource == null)
            return;

        try
        {
            CancelPendingConsumers();
        }
        finally
        {
            _consumeTaskSource.TrySetCanceled(cancellationToken);
        }
    }

    /// <summary>Faults a manually managed consume loop and cancels retained duplicate deliveries.</summary>
    /// <param name="exception">The transport failure reported by the consume loop.</param>
    protected void TrySetConsumeException(Exception exception)
    {
        ArgumentNullException.ThrowIfNull(exception);
        if (_consumeTaskSource == null)
            return;

        try
        {
            CancelPendingConsumers();
        }
        finally
        {
            _consumeTaskSource.TrySetException(exception);
        }
    }

    /// <summary>Waits for active deliveries and the transport consume loop during shutdown.</summary>
    /// <param name="context">The reason and cancellation token for shutdown.</param>
    /// <returns>A task that completes after both sources of activity have ended or shutdown is canceled.</returns>
    protected virtual async Task ActiveAndActualAgentsCompletedAsync(StopContext context)
    {
        var cancellationFailures = new ConcurrentQueue<Exception>();

        void CancelAndCaptureFailures()
        {
            try
            {
                CancelPendingConsumers();
            }
            catch (Exception exception)
            {
                cancellationFailures.Enqueue(exception);
            }
        }

        if (!IsIdle)
        {
            CancellationTokenSource? cancellationTokenSource = null;
            CancellationTokenRegistration? registration = null;

            if (_context.ConsumerStopTimeout != null)
            {
                cancellationTokenSource = new CancellationTokenSource(_context.ConsumerStopTimeout.Value, _context.GetTimeProvider());
                registration = cancellationTokenSource.Token.Register(CancelAndCaptureFailures);
            }

            try
            {
                await _deliveryComplete.Task.OrCanceledAsync(context.CancellationToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                try
                {
                    LogContext.Warning?.Log("Consumer stop canceled: {InputAddress}", _context.InputAddress);
                }
                catch
                {
                }
                finally
                {
                    CancelAndCaptureFailures();
                }
            }
            finally
            {
                registration?.Dispose();
                cancellationTokenSource?.Dispose();
            }
        }

        Task? consumeTask;
        lock (_consumeTaskLock)
            consumeTask = _consumeTask;

        if (consumeTask is not null)
        {
            try
            {
                await consumeTask.OrCanceledAsync(context.CancellationToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
            }
            catch (Exception e)
            {
                try
                {
                    LogContext.Warning?.Log(e, "Consumer stop faulted: {InputAddress}", _context.InputAddress);
                }
                catch
                {
                }
            }
        }

        // Registration disposal above joins the timeout callback before its failures are observed.
        if (!cancellationFailures.IsEmpty)
            throw new AggregateException("Retained delivery cancellation failed during shutdown.", cancellationFailures);
    }

    /// <summary>Determines whether deliveries with the supplied identity participate in duplicate coordination.</summary>
    /// <param name="key">The transport-specific delivery identity.</param>
    /// <returns><see langword="true" /> when the identity should be tracked; otherwise, <see langword="false" />.</returns>
    protected virtual bool IsTrackable(TKey key)
    {
        return true;
    }

    /// <summary>Dispatches the first delivery for an identity and retains later deliveries as settlement fallbacks.</summary>
    /// <typeparam name="TContext">The transport-specific receive context type.</typeparam>
    /// <param name="key">The transport-specific delivery identity.</param>
    /// <param name="context">The delivery passed to the receive pipeline.</param>
    /// <param name="receiveLockContext">The transport lock used to settle the delivery.</param>
    /// <returns>The dispatch task, or a completed task when a duplicate was retained.</returns>
    protected Task DispatchAsync<TContext>(TKey key, TContext context, ReceiveLockContext receiveLockContext)
        where TContext : BaseReceiveContext
    {
        ArgumentNullException.ThrowIfNull(key);
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(receiveLockContext);

        if (!IsTrackable(key))
        {
            return _dispatcher.DispatchAsync(context, receiveLockContext)
                ?? throw new InvalidOperationException("The receive pipe dispatcher returned no dispatch task.");
        }

        PendingReceiveLockContext pending;
        bool added;
        lock (_pendingLock)
        {
            if (!_pending.TryGetValue(key, out pending!) || pending.IsEmpty)
            {
                // A settled generation must not be revived while its dispatcher is still completing.
                pending = new PendingReceiveLockContext();
                _pending[key] = pending;
            }

            added = pending.Enqueue(context, receiveLockContext);
        }

        if (!added)
        {
            try
            {
                context.LogTransportDupe(key);
            }
            catch (Exception)
            {
            }
            return Task.CompletedTask;
        }

        return TrackDispatchAsync(key, pending, context);
    }

    async Task TrackDispatchAsync(TKey key, PendingReceiveLockContext pending, ReceiveContext context)
    {
        try
        {
            Task dispatchTask = _dispatcher.DispatchAsync(context, pending)
                ?? throw new InvalidOperationException("The receive pipe dispatcher returned no dispatch task.");
            await dispatchTask.ConfigureAwait(false);
        }
        catch (Exception dispatchFailure)
        {
            lock (_pendingLock)
            {
                if (_pending.TryGetValue(key, out var current) && ReferenceEquals(current, pending))
                    _pending.TryRemove(key, out _);
            }

            try
            {
                pending.Cancel();
            }
            catch (Exception cancellationFailure)
            {
                throw new AggregateException("Dispatch and retained delivery cancellation both failed.", dispatchFailure, cancellationFailure);
            }

            throw;
        }
        finally
        {
            lock (_pendingLock)
            {
                if (_pending.TryGetValue(key, out var current) && ReferenceEquals(current, pending) && pending.IsEmpty)
                    _pending.TryRemove(key, out _);
            }
        }
    }
}
