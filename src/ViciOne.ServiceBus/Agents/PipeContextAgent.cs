using System;
using System.Threading;
using System.Threading.Tasks;
using ViciOne.ServiceBus.Middleware;
using ViciOne.ServiceBus.Util;

namespace ViciOne.ServiceBus.Agents;

/// <summary>Owns a pipe context and disposes it when the supervised agent stops.</summary>
/// <typeparam name="TContext">The pipeline context carried by the member.</typeparam>
public sealed class PipeContextAgent<TContext> :
    Agent,
    IPipeContextAgent<TContext>
    where TContext : class, PipeContext
{
    readonly Task<TContext> _context;
    readonly TaskCompletionSource _disposeCompleted;
    int _disposeStarted;

    /// <summary>Initializes a new instance.</summary>
    /// <param name="context">The context associated with the operation.</param>
    public PipeContextAgent(TContext context)
        : this(Task.FromResult(context ?? throw new ArgumentNullException(nameof(context))))
    {
    }

    /// <summary>Initializes a new instance.</summary>
    /// <param name="context">The context associated with the operation.</param>
    public PipeContextAgent(Task<TContext> context)
    {
        _context = RequireContextAsync(context ?? throw new ArgumentNullException(nameof(context)));
        _disposeCompleted = TaskCompletionSources.Create();

        SetReady(_context);
    }

    bool PipeContextHandle<TContext>.IsDisposed => Volatile.Read(ref _disposeStarted) != 0;

    Task<TContext> PipeContextHandle<TContext>.Context => _context;

    /// <inheritdoc />
    public ValueTask DisposeAsync()
    {
        if (Interlocked.CompareExchange(ref _disposeStarted, 1, 0) == 0)
            _ = DisposeContextAsync();

        return new ValueTask(_disposeCompleted.Task);
    }

    /// <inheritdoc />
    protected override async Task StopAgentAsync(StopContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        await DisposeAsync().ConfigureAwait(false);
    }

    async Task DisposeContextAsync()
    {
        try
        {
            TContext context;
            try
            {
                context = await _context.ConfigureAwait(false);
            }
            catch
            {
                // A canceled or faulted creation task never transferred a context to this owner.
                _disposeCompleted.TrySetResult();
                return;
            }

            switch (context)
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
        finally
        {
            // Completion describes lifecycle termination; disposal failures remain on DisposeAsync.
            SetCompleted(Task.CompletedTask);
        }
    }

    static async Task<TContext> RequireContextAsync(Task<TContext> context)
    {
        return await context.ConfigureAwait(false)
            ?? throw new InvalidOperationException("The context task completed without a context.");
    }
}
