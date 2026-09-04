using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using ViciOne.ServiceBus.Internals;
using ViciOne.ServiceBus.Middleware;
using ViciOne.ServiceBus.Util;

namespace ViciOne.ServiceBus.Transports;

/// <summary>
/// Provides a consumer agent implementation.
/// </summary>
/// <typeparam name="TKey">The t key type.</typeparam>
public abstract class ConsumerAgent<TKey> :
    Agent,
    DeliveryMetrics
    where TKey : notnull
{
    readonly ReceiveEndpointContext _context;
    readonly TaskCompletionSource<bool> _deliveryComplete;
    readonly IReceivePipeDispatcher _dispatcher;
    readonly object _lock = new object();
    readonly ConcurrentDictionary<TKey, PendingReceiveLockContext> _pending;
    Task _consumeTask = null!;
    Task _consumeTaskObserver = null!;
    TaskCompletionSource<bool> _consumeTaskSource = null!;

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="context">The operation context.</param>
    /// <param name="equalityComparer">The equality comparer value.</param>
    protected ConsumerAgent(ReceiveEndpointContext context, IEqualityComparer<TKey>? equalityComparer = default)
    {
        _context = context;
        _deliveryComplete = TaskCompletionSources.Create<bool>();

        _pending = new ConcurrentDictionary<TKey, PendingReceiveLockContext>(equalityComparer ?? EqualityComparer<TKey>.Default);

        _dispatcher = context.CreateReceivePipeDispatcher();
        _dispatcher.ZeroActivity += HandleDeliveryCompleteAsync;
    }

    /// <summary>
    /// Gets the is idle value.
    /// </summary>
    protected bool IsIdle => ActiveDispatchCount == 0;

    /// <summary>
    /// Gets the active dispatch count value.
    /// </summary>
    protected long ActiveDispatchCount => _dispatcher.ActiveDispatchCount;

    /// <summary>
    /// Gets or sets the is graceful shutdown value.
    /// </summary>
    protected bool IsGracefulShutdown { get; private set; } = true;

    /// <summary>
    /// Gets the delivery count value.
    /// </summary>
    public long DeliveryCount => _dispatcher.DispatchCount;

    /// <summary>
    /// Gets the concurrent delivery count value.
    /// </summary>
    public int ConcurrentDeliveryCount => _dispatcher.MaxConcurrentDispatchCount;

    Task HandleDeliveryCompleteAsync()
    {
        if (IsStopping)
            _deliveryComplete.TrySetResult(true);

        return Task.CompletedTask;
    }

    /// <summary>
    /// Performs the try set manual consume task operation.
    /// </summary>
    protected void TrySetManualConsumeTask()
    {
        if (_consumeTask != null || _consumeTaskSource != null)
            return;

        lock (_lock)
        {
            if (_consumeTask != null || _consumeTaskSource != null)
                return;

            _consumeTaskSource = TaskCompletionSources.Create<bool>();
            SetConsumeTask(_consumeTaskSource.Task);
        }
    }

    /// <summary>
    /// Performs the try set consume task operation.
    /// </summary>
    /// <param name="consumeTask">The consume task value.</param>
    protected void TrySetConsumeTask(Task consumeTask)
    {
        if (_consumeTask != null)
            return;

        lock (_lock)
        {
            if (_consumeTask != null)
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

        IsGracefulShutdown = false;

        try
        {
            LogContext.SetCurrentIfNull(_context.LogContext);

            using var tokenSource = _context.StopTimeout.HasValue
                ? new CancellationTokenSource(_context.StopTimeout.Value)
                : new CancellationTokenSource();

            await this.StopAsync("Consume Loop Exited", tokenSource.Token).ConfigureAwait(false);
        }
        catch (Exception exception)
        {
            LogContext.Warning?.Log(exception, "Stop Faulted");
        }
    }

    /// <summary>
    /// Stops agent.
    /// </summary>
    /// <param name="context">The operation context.</param>
    /// <returns>The result of the operation.</returns>
    protected override Task StopAgentAsync(StopContext context)
    {
        LogContext.Debug?.Log("Consumer Stopping: {InputAddress} ({Reason})", _context.InputAddress, context.Reason);

        TrySetConsumeCompleted();

        SetCompleted(ActiveAndActualAgentsCompletedAsync(context));

        return Completed;
    }

    void CancelPendingConsumers()
    {
        foreach (var key in _pending.Keys)
        {
            if (_pending.TryRemove(key, out var context))
                context.Cancel();
        }
    }

    /// <summary>
    /// Performs the try set consume completed operation.
    /// </summary>
    protected void TrySetConsumeCompleted()
    {
        _consumeTaskSource?.TrySetResult(true);
    }

    /// <summary>
    /// Performs the try set consume canceled operation.
    /// </summary>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    protected void TrySetConsumeCanceled(CancellationToken cancellationToken = default)
    {
        if (_consumeTaskSource == null)
            return;

        CancelPendingConsumers();

        _consumeTaskSource.TrySetCanceled(cancellationToken);
    }

    /// <summary>
    /// Performs the try set consume exception operation.
    /// </summary>
    /// <param name="exception">The exception associated with the operation.</param>
    protected void TrySetConsumeException(Exception exception)
    {
        if (_consumeTaskSource == null)
            return;

        CancelPendingConsumers();

        _consumeTaskSource.TrySetException(exception);
    }

    /// <summary>
    /// Performs the active and actual agents completed operation.
    /// </summary>
    /// <param name="context">The operation context.</param>
    /// <returns>The result of the operation.</returns>
    protected virtual async Task ActiveAndActualAgentsCompletedAsync(StopContext context)
    {
        if (!IsIdle)
        {
            CancellationTokenSource? cancellationTokenSource = null;
            CancellationTokenRegistration? registration = null;

            if (_context.ConsumerStopTimeout != null)
            {
                cancellationTokenSource = new CancellationTokenSource(_context.ConsumerStopTimeout.Value);
                registration = cancellationTokenSource.Token.Register(CancelPendingConsumers);
            }

            try
            {
                await _deliveryComplete.Task.OrCanceledAsync(context.CancellationToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                LogContext.Warning?.Log("Consumer stop canceled: {InputAddress}", _context.InputAddress);

                CancelPendingConsumers();
            }
            finally
            {
                registration?.Dispose();
                cancellationTokenSource?.Dispose();
            }
        }

        if (_consumeTask == null)
            return;

        try
        {
            await _consumeTask.OrCanceledAsync(context.CancellationToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
        }
        catch (Exception e)
        {
            LogContext.Warning?.Log(e, "Consumer stop faulted: {InputAddress}", _context.InputAddress);
        }
    }

    /// <summary>
    /// Determines whether trackable.
    /// </summary>
    /// <param name="key">The key value.</param>
    /// <returns><see langword="true" /> when the condition is satisfied; otherwise, <see langword="false" />.</returns>
    protected virtual bool IsTrackable(TKey key)
    {
        return true;
    }

    /// <summary>
    /// Performs the dispatch operation.
    /// </summary>
    /// <typeparam name="TContext">The t context type.</typeparam>
    /// <param name="key">The key value.</param>
    /// <param name="context">The operation context.</param>
    /// <param name="receiveLockContext">The receive lock context value.</param>
    /// <returns>The result of the operation.</returns>
    protected Task DispatchAsync<TContext>(TKey key, TContext context, ReceiveLockContext receiveLockContext)
        where TContext : BaseReceiveContext
    {
        var added = false;
        var lockContext = receiveLockContext;

        if (IsTrackable(key))
        {
            lockContext = _pending.AddOrUpdate(key, _ =>
            {
                var current = new PendingReceiveLockContext();
                added = current.Enqueue(context, receiveLockContext);
                return current;
            }, (_, current) =>
            {
                added = current.Enqueue(context, receiveLockContext);
                return current;
            });

            if (!added)
            {
                context.LogTransportDupe(key);
                return Task.CompletedTask;
            }
        }

        var dispatchTask = _dispatcher.DispatchAsync(context, lockContext);

        return added ? TrackDispatchAsync(dispatchTask, key) : dispatchTask;
    }

    async Task TrackDispatchAsync(Task dispatchTask, TKey key)
    {
        try
        {
            await dispatchTask.ConfigureAwait(false);
        }
        finally
        {
            if (_pending.TryGetValue(key, out var value) && value.IsEmpty)
                _pending.TryRemove(key, out _);
        }
    }
}
