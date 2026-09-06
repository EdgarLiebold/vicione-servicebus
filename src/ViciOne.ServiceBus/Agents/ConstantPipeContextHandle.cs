using System;
using System.Threading;
using System.Threading.Tasks;
using ViciOne.ServiceBus.Util;

namespace ViciOne.ServiceBus.Agents;

/// <summary>Owns the lifetime of an already available pipe context.</summary>
/// <typeparam name="TContext">The pipeline context carried by the member.</typeparam>
public sealed class ConstantPipeContextHandle<TContext> :
    PipeContextHandle<TContext>
    where TContext : class, PipeContext
{
    readonly TContext _context;
    readonly TaskCompletionSource _disposeCompleted;
    int _disposeStarted;

    /// <summary>Initializes a new instance.</summary>
    /// <param name="context">The context associated with the operation.</param>
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

    bool PipeContextHandle<TContext>.IsDisposed => Volatile.Read(ref _disposeStarted) != 0;

    /// <summary>Gets the context.</summary>
    public Task<TContext> Context { get; }

    async Task DisposeContextAsync()
    {
        try
        {
            switch (_context)
            {
                case IAsyncDisposable asyncDisposable:
                    await asyncDisposable.DisposeAsync().ConfigureAwait(false);
                    break;
                case IDisposable disposable:
                    disposable.Dispose();
                    break;
            }

            _disposeCompleted.TrySetResult();
        }
        catch (Exception exception)
        {
            _disposeCompleted.TrySetException(exception);
        }
    }
}
