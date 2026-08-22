namespace ViciOne.ServiceBus.Util;

using System;
using System.Linq;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;


/// <summary>
/// Executes asynchronous work with a fixed concurrency limit and optional bounded queue capacity.
/// </summary>
public sealed class TaskExecutor :
    IAsyncDisposable
{
    readonly Task _readerTask;
    readonly Channel<IFuture> _taskChannel;
    int _disposeState;

    public TaskExecutor(int concurrencyLimit = 1)
    {
        if (concurrencyLimit < 1)
            throw new ArgumentOutOfRangeException(nameof(concurrencyLimit), concurrencyLimit, "Must be >= 1");

        _taskChannel = Channel.CreateUnbounded<IFuture>(new UnboundedChannelOptions
        {
            AllowSynchronousContinuations = true,
            SingleReader = concurrencyLimit == 1,
            SingleWriter = false
        });

        _readerTask = concurrencyLimit == 1
            ? Task.Run(() => SingleReader())
            : Task.WhenAll(Enumerable.Range(0, concurrencyLimit).Select(_ => Task.Run(() => MultipleReader())));
    }

    public TaskExecutor(int prefetchCount, int concurrencyLimit = 1)
    {
        if (prefetchCount < 1)
            throw new ArgumentOutOfRangeException(nameof(prefetchCount), prefetchCount, "Must be >= 1");

        if (concurrencyLimit < 1)
            throw new ArgumentOutOfRangeException(nameof(concurrencyLimit), concurrencyLimit, "Must be >= 1");

        _taskChannel = Channel.CreateBounded<IFuture>(new BoundedChannelOptions(prefetchCount)
        {
            AllowSynchronousContinuations = true,
            SingleReader = concurrencyLimit == 1,
            SingleWriter = false
        });

        _readerTask = concurrencyLimit == 1
            ? Task.Run(() => SingleReader())
            : Task.WhenAll(Enumerable.Range(0, concurrencyLimit).Select(_ => Task.Run(() => MultipleReader())));
    }

    public async ValueTask DisposeAsync()
    {
        if (Interlocked.Exchange(ref _disposeState, 1) == 0)
            _taskChannel.Writer.TryComplete();

        await _readerTask.ConfigureAwait(false);
    }

    public async Task Run(Action method, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(method);
        var future = new ActionFuture(method, cancellationToken);

        await Enqueue(future, cancellationToken).ConfigureAwait(false);

        await future.Completed.ConfigureAwait(false);
    }

    public async Task Run(Func<Task> method, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(method);
        var future = new TaskFuture(method, cancellationToken);

        await Enqueue(future, cancellationToken).ConfigureAwait(false);

        await future.Completed.ConfigureAwait(false);
    }

    public async Task RunValueTask(Func<ValueTask> method, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(method);
        var future = new ValueTaskFuture(method, cancellationToken);

        await Enqueue(future, cancellationToken).ConfigureAwait(false);

        await future.Completed.ConfigureAwait(false);
    }

    public async Task Push(Action method, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(method);
        var future = new ActionFuture(method, cancellationToken);

        await Enqueue(future, cancellationToken).ConfigureAwait(false);
    }

    public async Task Push(Func<Task> method, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(method);
        var future = new TaskFuture(method, cancellationToken);

        await Enqueue(future, cancellationToken).ConfigureAwait(false);
    }

    public async Task PushValueTask(Func<ValueTask> method, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(method);
        var future = new ValueTaskFuture(method, cancellationToken);

        await Enqueue(future, cancellationToken).ConfigureAwait(false);
    }

    public async Task<T> Run<T>(Func<T> method, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(method);
        var future = new FuncFuture<T>(method, cancellationToken);

        await Enqueue(future, cancellationToken).ConfigureAwait(false);

        return await future.Completed.ConfigureAwait(false);
    }

    public async Task<T> Run<T>(Func<Task<T>> method, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(method);
        var future = new TaskFuture<T>(method, cancellationToken);

        await Enqueue(future, cancellationToken).ConfigureAwait(false);

        return await future.Completed.ConfigureAwait(false);
    }

    public async Task<T> RunValueTask<T>(Func<ValueTask<T>> method, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(method);
        var future = new ValueTaskFuture<T>(method, cancellationToken);

        await Enqueue(future, cancellationToken).ConfigureAwait(false);

        return await future.Completed.ConfigureAwait(false);
    }

    async ValueTask Enqueue(IFuture future, CancellationToken cancellationToken)
    {
        ThrowIfDisposed();

        try
        {
            await _taskChannel.Writer.WriteAsync(future, cancellationToken).ConfigureAwait(false);
        }
        catch (ChannelClosedException) when (Volatile.Read(ref _disposeState) != 0)
        {
            throw new ObjectDisposedException(nameof(TaskExecutor));
        }
    }

    void ThrowIfDisposed()
    {
        if (Volatile.Read(ref _disposeState) != 0)
            throw new ObjectDisposedException(nameof(TaskExecutor));
    }

    async Task MultipleReader()
    {
        try
        {
            while (await _taskChannel.Reader.WaitToReadAsync().ConfigureAwait(false))
            {
                if (!_taskChannel.Reader.TryRead(out var future))
                    continue;

                await future.Run().ConfigureAwait(false);
            }
        }
        catch (OperationCanceledException)
        {
        }
        catch (Exception exception)
        {
            LogContext.Warning?.Log(exception, "MultipleReader faulted");
        }
    }

    async Task SingleReader()
    {
        try
        {
            while (await _taskChannel.Reader.WaitToReadAsync().ConfigureAwait(false))
            {
                if (!_taskChannel.Reader.TryPeek(out var future))
                    continue;

                await future.Run().ConfigureAwait(false);

                await _taskChannel.Reader.ReadAsync().ConfigureAwait(false);
            }
        }
        catch (OperationCanceledException)
        {
        }
        catch (Exception exception)
        {
            LogContext.Warning?.Log(exception, "SingleReader faulted");
        }
    }


    interface IFuture
    {
        ValueTask Run();
    }


    abstract class BaseFuture<T>
    {
        protected readonly CancellationToken CancellationToken;
        protected readonly TaskCompletionSource<T> Source;

        protected BaseFuture(CancellationToken cancellationToken)
        {
            CancellationToken = cancellationToken;

            Source = new TaskCompletionSource<T>(TaskCreationOptions.RunContinuationsAsynchronously);
        }

        public Task<T> Completed => Source.Task;

        protected bool TrySetCanceled()
        {
            if (!CancellationToken.IsCancellationRequested)
                return false;

            Source.TrySetCanceled(CancellationToken);
            return true;
        }

        protected void SetException(Exception exception)
        {
            if (exception is OperationCanceledException canceled)
                Source.TrySetCanceled(canceled.CancellationToken);
            else
                Source.TrySetException(exception);
        }
    }


    abstract class BaseFuture :
        BaseFuture<bool>
    {
        protected BaseFuture(CancellationToken cancellationToken)
            : base(cancellationToken)
        {
        }
    }


    sealed class TaskFuture<T> :
        BaseFuture<T>,
        IFuture
    {
        readonly Func<Task<T>> _method;

        public TaskFuture(Func<Task<T>> method, CancellationToken cancellationToken)
            : base(cancellationToken)
        {
            _method = method;
        }

        public async ValueTask Run()
        {
            if (TrySetCanceled())
                return;

            try
            {
                var result = await _method().ConfigureAwait(false);

                Source.TrySetResult(result);
            }
            catch (Exception exception)
            {
                SetException(exception);
            }
        }
    }


    sealed class ValueTaskFuture<T> :
        BaseFuture<T>,
        IFuture
    {
        readonly Func<ValueTask<T>> _method;

        public ValueTaskFuture(Func<ValueTask<T>> method, CancellationToken cancellationToken)
            : base(cancellationToken)
        {
            _method = method;
        }

        public async ValueTask Run()
        {
            if (TrySetCanceled())
                return;

            try
            {
                var result = await _method().ConfigureAwait(false);

                Source.TrySetResult(result);
            }
            catch (Exception exception)
            {
                SetException(exception);
            }
        }
    }


    sealed class TaskFuture :
        BaseFuture,
        IFuture
    {
        readonly Func<Task> _method;

        public TaskFuture(Func<Task> method, CancellationToken cancellationToken)
            : base(cancellationToken)
        {
            _method = method;
        }

        public async ValueTask Run()
        {
            if (TrySetCanceled())
                return;

            try
            {
                await _method().ConfigureAwait(false);

                Source.TrySetResult(true);
            }
            catch (Exception exception)
            {
                SetException(exception);
            }
        }
    }


    sealed class ValueTaskFuture :
        BaseFuture,
        IFuture
    {
        readonly Func<ValueTask> _method;

        public ValueTaskFuture(Func<ValueTask> method, CancellationToken cancellationToken)
            : base(cancellationToken)
        {
            _method = method;
        }

        public async ValueTask Run()
        {
            if (TrySetCanceled())
                return;

            try
            {
                await _method().ConfigureAwait(false);

                Source.TrySetResult(true);
            }
            catch (Exception exception)
            {
                SetException(exception);
            }
        }
    }


    sealed class FuncFuture<T> :
        BaseFuture<T>,
        IFuture
    {
        readonly Func<T> _method;

        public FuncFuture(Func<T> method, CancellationToken cancellationToken)
            : base(cancellationToken)
        {
            _method = method;
        }

        public ValueTask Run()
        {
            if (TrySetCanceled())
                return default;

            try
            {
                var result = _method();

                Source.TrySetResult(result);
            }
            catch (Exception exception)
            {
                SetException(exception);
            }

            return default;
        }
    }


    sealed class ActionFuture :
        BaseFuture,
        IFuture
    {
        readonly Action _method;

        public ActionFuture(Action method, CancellationToken cancellationToken)
            : base(cancellationToken)
        {
            _method = method;
        }

        public ValueTask Run()
        {
            if (TrySetCanceled())
                return default;

            try
            {
                _method();

                Source.TrySetResult(true);
            }
            catch (Exception exception)
            {
                SetException(exception);
            }

            return default;
        }
    }
}
