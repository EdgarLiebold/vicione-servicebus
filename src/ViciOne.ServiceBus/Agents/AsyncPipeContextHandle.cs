using System;
using System.Threading.Tasks;
using ViciOne.ServiceBus.Util;

namespace ViciOne.ServiceBus.Agents;

/// <summary>Tracks the availability and lifetime of an asynchronously created pipe context.</summary>
/// <typeparam name="TContext">The context type.</typeparam>
public sealed class AsyncPipeContextHandle<TContext> :
    IAsyncPipeContextHandle<TContext>
    where TContext : class, PipeContext
{
    readonly TaskCompletionSource<TContext> _context;
    readonly TaskCompletionSource _inactive;

    /// <summary>Initializes a new instance.</summary>
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

        _context.TrySetResult(context);

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

        _inactive.TrySetException(exception);

        return Task.CompletedTask;
    }

    ValueTask IAsyncDisposable.DisposeAsync()
    {
        _inactive.TrySetResult();

        return default;
    }
}
