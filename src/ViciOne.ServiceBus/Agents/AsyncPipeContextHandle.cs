using System;
using System.Threading.Tasks;
using ViciOne.ServiceBus.Util;

namespace ViciOne.ServiceBus.Agents;

/// <summary>
/// An asynchronously pipe context handle, which can be completed.
/// </summary>
/// <typeparam name="TContext">The context type</typeparam>
public class AsyncPipeContextHandle<TContext> :
    IAsyncPipeContextHandle<TContext>
    where TContext : class, PipeContext
{
    readonly TaskCompletionSource<TContext> _context;
    readonly TaskCompletionSource<DateTime> _inactive;
    readonly TimeProvider _timeProvider;

    /// <summary>
    /// Creates the handle
    /// </summary>
    public AsyncPipeContextHandle(TimeProvider? timeProvider = null)
    {
        _context = TaskCompletionSources.Create<TContext>();
        _inactive = TaskCompletionSources.Create<DateTime>();
        _timeProvider = timeProvider ?? TimeProvider.System;
    }

    bool PipeContextHandle<TContext>.IsDisposed => _inactive.Task.IsCompleted;

    Task<TContext> PipeContextHandle<TContext>.Context => _context.Task;

    Task IAsyncPipeContextHandle<TContext>.CreatedAsync(TContext context, CancellationToken cancellationToken)
    {
        _context.SetResult(context);

        return Task.CompletedTask;
    }

    Task IAsyncPipeContextHandle<TContext>.CreateCanceledAsync(CancellationToken cancellationToken)
    {
        _context.SetCanceled();

        return Task.CompletedTask;
    }

    Task IAsyncPipeContextHandle<TContext>.CreateFaultedAsync(Exception exception, CancellationToken cancellationToken)
    {
        _context.SetException(exception);

        return Task.CompletedTask;
    }

    Task IAsyncPipeContextHandle<TContext>.FaultedAsync(Exception exception, CancellationToken cancellationToken)
    {
        _inactive.TrySetException(exception);

        return Task.CompletedTask;
    }

    ValueTask IAsyncDisposable.DisposeAsync()
    {
        _inactive.TrySetResult(_timeProvider.GetUtcNow().UtcDateTime);

        return default;
    }
}
