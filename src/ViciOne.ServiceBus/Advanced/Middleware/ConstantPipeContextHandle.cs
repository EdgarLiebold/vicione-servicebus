using System;
using System.Threading;
using System.Threading.Tasks;
using ViciOne.ServiceBus.Util;

namespace ViciOne.ServiceBus.Advanced.Middleware;

/// <summary>Owns the lifetime of an already available pipe context.</summary>
/// <typeparam name="TContext">The pipe context type.</typeparam>
public sealed class ConstantPipeContextHandle<TContext> :
    IPipeContextHandle<TContext>
    where TContext : class, PipeContext
{
    readonly TContext _context;
    readonly TaskCompletionSource _disposeCompleted;
    int _disposeStarted;

    /// <summary>Initializes a handle that owns an already available context.</summary>
    /// <param name="context">The context to expose and dispose.</param>
    public ConstantPipeContextHandle(TContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _disposeCompleted = TaskCompletionSources.Create();

        Context = Task.FromResult(context);
    }

    ValueTask IAsyncDisposable.DisposeAsync()
    {
        if (Interlocked.CompareExchange(ref _disposeStarted, 1, 0) == 0)
            _ = DisposeContextAsync();

        return new ValueTask(_disposeCompleted.Task);
    }

    bool IPipeContextHandle<TContext>.IsDisposed => Volatile.Read(ref _disposeStarted) != 0;

    /// <summary>Gets the completed task containing the owned context.</summary>
    public Task<TContext> Context { get; }

    async Task DisposeContextAsync()
    {
        try
        {
            await PipeContextDisposer.DisposeAsync(_context).ConfigureAwait(false);

            _disposeCompleted.TrySetResult();
        }
        catch (Exception exception)
        {
            _disposeCompleted.TrySetException(exception);
        }
    }
}
