using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace ViciOne.ServiceBus.Util;
/// <summary>Owns a bounded asynchronous work queue with a fixed number of concurrent workers.</summary>
public sealed class TaskExecutor :
    IAsyncDisposable
{
    readonly Channel<IWorkItem> _channel;
    readonly Task _workers;
    int _lifecycleState;

    /// <summary>Creates an executor whose bounded capacity is derived from its worker count.</summary>
    /// <param name="concurrencyLimit">The maximum number of delegates that may execute concurrently.</param>
    public TaskExecutor(int concurrencyLimit = 1)
        : this(GetDefaultCapacity(concurrencyLimit), concurrencyLimit)
    {
    }

    /// <summary>Creates an executor with explicit queue and worker limits.</summary>
    /// <param name="capacity">The maximum number of delegates waiting for a worker.</param>
    /// <param name="concurrencyLimit">The maximum number of delegates that may execute concurrently.</param>
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

    /// <summary>Stops new admissions and waits for every accepted delegate to finish.</summary>
    /// <returns>A task that completes when all workers have drained and stopped.</returns>
    public async ValueTask DisposeAsync()
    {
        if (Interlocked.CompareExchange(ref _lifecycleState, 1, 0) == 0)
            _channel.Writer.TryComplete();

        await _workers.ConfigureAwait(false);
        Volatile.Write(ref _lifecycleState, 2);
    }

    /// <summary>Queues an action and completes after that action finishes.</summary>
    /// <param name="method">The action to execute.</param>
    /// <param name="cancellationToken">Cancels queue admission or execution before the action begins.</param>
    /// <returns>A task that represents the action's completion.</returns>
    public Task ExecuteAsync(Action method, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(method);

        return ExecuteAsync(() =>
        {
            method();
            return Task.CompletedTask;
        }, cancellationToken);
    }

    /// <summary>Queues an asynchronous delegate and completes after that delegate finishes.</summary>
    /// <param name="method">The asynchronous delegate to execute.</param>
    /// <param name="cancellationToken">Cancels queue admission or execution before the delegate begins.</param>
    /// <returns>A task that represents the delegate's completion.</returns>
    public async Task ExecuteAsync(Func<Task> method, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(method);

        var item = new CompletionWorkItem(method, cancellationToken);
        await EnqueueCoreAsync(item, cancellationToken).ConfigureAwait(false);
        await item.Completed.ConfigureAwait(false);
    }

    /// <summary>Queues a value-task delegate and completes after that delegate finishes.</summary>
    /// <param name="method">The value-task delegate to execute.</param>
    /// <param name="cancellationToken">Cancels queue admission or execution before the delegate begins.</param>
    /// <returns>A task that represents the delegate's completion.</returns>
    public Task ExecuteValueTaskAsync(Func<ValueTask> method, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(method);
        return ExecuteAsync(async () => await method().ConfigureAwait(false), cancellationToken);
    }

    /// <summary>Queues a function and returns its result after execution.</summary>
    /// <typeparam name="T">The result type.</typeparam>
    /// <param name="method">The function to execute.</param>
    /// <param name="cancellationToken">Cancels queue admission or execution before the function begins.</param>
    /// <returns>A task containing the function result.</returns>
    public Task<T> ExecuteAsync<T>(Func<T> method, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(method);

        return ExecuteAsync(() => Task.FromResult(method()), cancellationToken);
    }

    /// <summary>Queues an asynchronous function and returns its result after execution.</summary>
    /// <typeparam name="T">The result type.</typeparam>
    /// <param name="method">The asynchronous function to execute.</param>
    /// <param name="cancellationToken">Cancels queue admission or execution before the function begins.</param>
    /// <returns>A task containing the function result.</returns>
    public async Task<T> ExecuteAsync<T>(Func<Task<T>> method, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(method);

        var item = new CompletionWorkItem<T>(method, cancellationToken);
        await EnqueueCoreAsync(item, cancellationToken).ConfigureAwait(false);
        return await item.Completed.ConfigureAwait(false);
    }

    /// <summary>Queues a value-task function and returns its result after execution.</summary>
    /// <typeparam name="T">The result type.</typeparam>
    /// <param name="method">The value-task function to execute.</param>
    /// <param name="cancellationToken">Cancels queue admission or execution before the function begins.</param>
    /// <returns>A task containing the function result.</returns>
    public Task<T> ExecuteValueTaskAsync<T>(Func<ValueTask<T>> method, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(method);
        return ExecuteAsync(async () => await method().ConfigureAwait(false), cancellationToken);
    }

    /// <summary>
    /// Enqueues work and completes once the bounded queue accepted it. Work failures are owned and
    /// logged by the executor because no caller awaits the work result.
    /// </summary>
    /// <param name="method">The action whose execution ownership transfers to the executor.</param>
    /// <param name="cancellationToken">Cancels only the queue-admission wait.</param>
    /// <returns>A task that completes when the bounded queue accepts the action.</returns>
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
    /// <param name="method">The delegate whose execution ownership transfers to the executor.</param>
    /// <param name="cancellationToken">Cancels only the queue-admission wait.</param>
    /// <returns>A task that completes when the bounded queue accepts the delegate.</returns>
    public async Task EnqueueAsync(Func<Task> method, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(method);
        await EnqueueCoreAsync(new QueuedWorkItem(method), cancellationToken).ConfigureAwait(false);
    }

    /// <summary>Transfers a value-task delegate to the bounded queue.</summary>
    /// <param name="method">The delegate whose execution ownership transfers to the executor.</param>
    /// <param name="cancellationToken">Cancels only the queue-admission wait.</param>
    /// <returns>A task that completes when the bounded queue accepts the delegate.</returns>
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
    /// <param name="method">The delegate whose execution ownership transfers to the executor.</param>
    /// <param name="cancellationToken">Cancels only the queue-admission wait.</param>
    public void EnqueueBlocking(Func<Task> method, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(method);
        ThrowIfNotAcceptingWork();

        var item = new QueuedWorkItem(method);
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
        readonly Func<Task> _method;

        public QueuedWorkItem(Func<Task> method)
        {
            _method = method;
        }

        public async ValueTask ExecuteAsync()
        {
            try
            {
                await _method().ConfigureAwait(false);
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
