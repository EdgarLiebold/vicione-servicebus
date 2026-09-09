using System;
using System.Threading.Tasks;
using ViciOne.ServiceBus.Util;

namespace ViciOne.ServiceBus.Advanced.Middleware;

/// <summary>Tracks the availability and lifetime of an asynchronously created pipe context.</summary>
/// <typeparam name="TContext">The pipe context type.</typeparam>
public sealed class AsyncPipeContextHandle<TContext> :
    IAsyncPipeContextHandle<TContext>
    where TContext : class, PipeContext
{
    readonly TaskCompletionSource<TContext> _context;
    readonly TaskCompletionSource _inactive;

    /// <summary>Initializes a handle with pending creation and lifetime signals.</summary>
    public AsyncPipeContextHandle()
    {
        _context = TaskCompletionSources.Create<TContext>();
        _inactive = TaskCompletionSources.Create();
    }

    bool IPipeContextHandle<TContext>.IsDisposed => _inactive.Task.IsCompleted;

    Task<TContext> IPipeContextHandle<TContext>.Context => _context.Task;

    /// <summary>Gets the task that completes when the handle is disposed or faults when the context is invalidated.</summary>
    public Task Completion => _inactive.Task;

    Task IAsyncPipeContextHandle<TContext>.CreatedAsync(TContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        if (!_context.TrySetResult(context))
            return PipeContextDisposer.DisposeAsync(context);

        return Task.CompletedTask;
    }

    Task IAsyncPipeContextHandle<TContext>.CreateCanceledAsync(CancellationToken cancellationToken)
    {
        _context.TrySetCanceled(cancellationToken);

        return Task.CompletedTask;
    }

    Task IAsyncPipeContextHandle<TContext>.CreateFaultedAsync(Exception exception)
    {
        ArgumentNullException.ThrowIfNull(exception);

        _context.TrySetException(exception);

        return Task.CompletedTask;
    }

    Task IAsyncPipeContextHandle<TContext>.FaultedAsync(Exception exception)
    {
        ArgumentNullException.ThrowIfNull(exception);

        _context.TrySetException(exception);
        _inactive.TrySetException(exception);

        return Task.CompletedTask;
    }

    ValueTask IAsyncDisposable.DisposeAsync()
    {
        _context.TrySetCanceled();
        _inactive.TrySetResult();

        return default;
    }
}
