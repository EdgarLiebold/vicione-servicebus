using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace ViciOne.ServiceBus.Util;
/// <summary>
/// Executes asynchronous work with bounded backpressure and a fixed concurrency limit.
/// </summary>
public sealed class TaskExecutor :
    IAsyncDisposable
{
    readonly Channel<IWorkItem> _channel;
    readonly Task _workers;
    int _lifecycleState;

    /// <summary>
    /// Creates an executor using a bounded default queue sized relative to the concurrency limit.
    /// </summary>
    public TaskExecutor(int concurrencyLimit = 1)
        : this(GetDefaultCapacity(concurrencyLimit), concurrencyLimit)
    {
    }

    /// <summary>
    /// Creates an executor with an explicit queue capacity and concurrency limit.
    /// </summary>
    public TaskExecutor(int capacity, int concurrencyLimit)
    {
        if (capacity < 1)
            throw new ArgumentOutOfRangeException(nameof(capacity), capacity, "Must be >= 1");
        if (concurrencyLimit < 1)
            throw new ArgumentOutOfRangeException(nameof(concurrencyLimit), concurrencyLimit, "Must be >= 1");

        _channel = Channel.CreateBounded<IWorkItem>(new BoundedChannelOptions(capacity)
        {
            AllowSynchronousContinuations = false,
            FullMode = BoundedChannelFullMode.Wait,
            SingleReader = concurrencyLimit == 1,
            SingleWriter = false
        });

        // Async worker methods start immediately and naturally yield at the first incomplete await.
        // Task.Run would only add an unnecessary ThreadPool scheduling hop.
        Task[] workers = Enumerable.Range(0, concurrencyLimit).Select(_ => RunWorkerAsync()).ToArray();
        _workers = workers.Length == 1 ? workers[0] : Task.WhenAll(workers);
    }

    public async ValueTask DisposeAsync()
    {
        if (Interlocked.CompareExchange(ref _lifecycleState, 1, 0) == 0)
            _channel.Writer.TryComplete();

        await _workers.ConfigureAwait(false);
        Volatile.Write(ref _lifecycleState, 2);
    }

    public Task ExecuteAsync(Action method, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(method);

        return ExecuteAsync(() =>
        {
            method();
            return Task.CompletedTask;
        }, cancellationToken);
    }

    public async Task ExecuteAsync(Func<Task> method, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(method);

        var item = new CompletionWorkItem(method, cancellationToken);
        await EnqueueCoreAsync(item, cancellationToken).ConfigureAwait(false);
        await item.Completed.ConfigureAwait(false);
    }

    public Task ExecuteValueTaskAsync(Func<ValueTask> method, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(method);
        return ExecuteAsync(async () => await method().ConfigureAwait(false), cancellationToken);
    }

    public Task<T> ExecuteAsync<T>(Func<T> method, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(method);

        return ExecuteAsync(() => Task.FromResult(method()), cancellationToken);
    }

    public async Task<T> ExecuteAsync<T>(Func<Task<T>> method, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(method);

        var item = new CompletionWorkItem<T>(method, cancellationToken);
        await EnqueueCoreAsync(item, cancellationToken).ConfigureAwait(false);
        return await item.Completed.ConfigureAwait(false);
    }

    public Task<T> ExecuteValueTaskAsync<T>(Func<ValueTask<T>> method, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(method);
        return ExecuteAsync(async () => await method().ConfigureAwait(false), cancellationToken);
    }

    /// <summary>
    /// Enqueues work and completes once the bounded queue accepted it. Work failures are owned and
    /// logged by the executor because no caller awaits the work result.
    /// </summary>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <param name="method">The method used by the operation.</param>
    public Task EnqueueAsync(Action method, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(method);

        return EnqueueAsync(() =>
        {
            method();
            return Task.CompletedTask;
        }, cancellationToken);
    }

    /// <summary>
    /// Enqueues work and completes once the bounded queue accepted it. Work failures are owned and
    /// logged by the executor because no caller awaits the work result.
    /// </summary>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <param name="method">The method used by the operation.</param>
    public async Task EnqueueAsync(Func<Task> method, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(method);
        await EnqueueCoreAsync(new QueuedWorkItem(method, cancellationToken), cancellationToken).ConfigureAwait(false);
    }

    public Task EnqueueValueTaskAsync(Func<ValueTask> method, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(method);
        return EnqueueAsync(async () => await method().ConfigureAwait(false), cancellationToken);
    }

    /// <summary>
    /// Synchronous backpressure boundary for callback APIs which cannot return a Task (for example
    /// Apache.NMS message listeners). This blocks only until the bounded queue accepts the work; it
    /// never polls and it does not wait for message processing to finish.
    /// </summary>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <param name="method">The method used by the operation.</param>
    public void EnqueueBlocking(Func<Task> method, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(method);
        ThrowIfNotAcceptingWork();

        var item = new QueuedWorkItem(method, cancellationToken);
        ValueTask write = _channel.Writer.WriteAsync(item, cancellationToken);
        if (!write.IsCompletedSuccessfully)
        {
            try
            {
                write.AsTask().GetAwaiter().GetResult();
            }
            catch (ChannelClosedException) when (Volatile.Read(ref _lifecycleState) != 0)
            {
                throw new ObjectDisposedException(nameof(TaskExecutor));
            }
        }
    }

    async ValueTask EnqueueCoreAsync(IWorkItem item, CancellationToken cancellationToken)
    {
        ThrowIfNotAcceptingWork();

        try
        {
            await _channel.Writer.WriteAsync(item, cancellationToken).ConfigureAwait(false);
        }
        catch (ChannelClosedException) when (Volatile.Read(ref _lifecycleState) != 0)
        {
            throw new ObjectDisposedException(nameof(TaskExecutor));
        }
    }

    async Task RunWorkerAsync()
    {
        await foreach (IWorkItem item in _channel.Reader.ReadAllAsync().ConfigureAwait(false))
            await item.ExecuteAsync().ConfigureAwait(false);
    }

    void ThrowIfNotAcceptingWork()
    {
        if (Volatile.Read(ref _lifecycleState) != 0)
            throw new ObjectDisposedException(nameof(TaskExecutor));
    }

    static int GetDefaultCapacity(int concurrencyLimit)
    {
        if (concurrencyLimit < 1)
            throw new ArgumentOutOfRangeException(nameof(concurrencyLimit), concurrencyLimit, "Must be >= 1");

        // Keep the default deliberately bounded for edge deployments while still allowing a short
        // burst ahead of the workers. High-throughput transports pass their own prefetch capacity.
        return Math.Max(32, checked(concurrencyLimit * 8));
    }


    interface IWorkItem
    {
        ValueTask ExecuteAsync();
    }


    sealed class QueuedWorkItem :
        IWorkItem
    {
        readonly CancellationToken _cancellationToken;
        readonly Func<Task> _method;

        public QueuedWorkItem(Func<Task> method, CancellationToken cancellationToken)
        {
            _method = method;
            _cancellationToken = cancellationToken;
        }

        public async ValueTask ExecuteAsync()
        {
            if (_cancellationToken.IsCancellationRequested)
                return;

            try
            {
                await _method().ConfigureAwait(false);
            }
            catch (OperationCanceledException) when (_cancellationToken.IsCancellationRequested)
            {
            }
            catch (Exception exception)
            {
                // Queued work intentionally has no result task. The executor therefore owns failures
                // instead of manufacturing an unobserved TaskCompletionSource exception.
                TryLog(exception);
            }
        }

        static void TryLog(Exception exception)
        {
            try
            {
                LogContext.Warning?.Log(exception, "Queued task execution faulted");
            }
            catch
            {
                // A secondary observation failure cannot terminate a worker or strand accepted work.
            }
        }
    }


    sealed class CompletionWorkItem :
        IWorkItem
    {
        readonly CancellationToken _cancellationToken;
        readonly TaskCompletionSource _completed;
        readonly Func<Task> _method;

        public CompletionWorkItem(Func<Task> method, CancellationToken cancellationToken)
        {
            _method = method;
            _cancellationToken = cancellationToken;
            _completed = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        }

        public Task Completed => _completed.Task;

        public async ValueTask ExecuteAsync()
        {
            if (_cancellationToken.IsCancellationRequested)
            {
                _completed.TrySetCanceled(_cancellationToken);
                return;
            }

            try
            {
                await _method().ConfigureAwait(false);
                _completed.TrySetResult();
            }
            catch (OperationCanceledException exception)
            {
                _completed.TrySetCanceled(exception.CancellationToken);
            }
            catch (Exception exception)
            {
                _completed.TrySetException(exception);
            }
        }
    }


    sealed class CompletionWorkItem<T> :
        IWorkItem
    {
        readonly CancellationToken _cancellationToken;
        readonly TaskCompletionSource<T> _completed;
        readonly Func<Task<T>> _method;

        public CompletionWorkItem(Func<Task<T>> method, CancellationToken cancellationToken)
        {
            _method = method;
            _cancellationToken = cancellationToken;
            _completed = new TaskCompletionSource<T>(TaskCreationOptions.RunContinuationsAsynchronously);
        }

        public Task<T> Completed => _completed.Task;

        public async ValueTask ExecuteAsync()
        {
            if (_cancellationToken.IsCancellationRequested)
            {
                _completed.TrySetCanceled(_cancellationToken);
                return;
            }

            try
            {
                T result = await _method().ConfigureAwait(false);
                _completed.TrySetResult(result);
            }
            catch (OperationCanceledException exception)
            {
                _completed.TrySetCanceled(exception.CancellationToken);
            }
            catch (Exception exception)
            {
                _completed.TrySetException(exception);
            }
        }
    }
}
