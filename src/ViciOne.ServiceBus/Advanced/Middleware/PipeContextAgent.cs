using System;
using System.Threading;
using System.Threading.Tasks;
using ViciOne.ServiceBus.Internals;

namespace ViciOne.ServiceBus.Advanced.Middleware;

/// <summary>Owns a pipe context and disposes it when the supervised agent stops.</summary>
/// <typeparam name="TContext">The pipe context type.</typeparam>
public sealed class PipeContextAgent<TContext> :
    Agent,
    IPipeContextAgent<TContext>
    where TContext : class, PipeContext
{
    readonly Task<TContext> _context;
    readonly object _disposeLock;
    Task? _disposeTask;
    int _disposeStarted;

    /// <summary>Initializes an agent that owns an already available context.</summary>
    /// <param name="context">The context to expose and dispose.</param>
    public PipeContextAgent(TContext context)
        : this(Task.FromResult(context ?? throw new ArgumentNullException(nameof(context))))
    {
    }

    /// <summary>Initializes an agent that owns a context supplied asynchronously.</summary>
    /// <param name="context">The task that supplies the context to expose and dispose.</param>
    public PipeContextAgent(Task<TContext> context)
    {
        _context = RequireContextAsync(context ?? throw new ArgumentNullException(nameof(context)));
        _disposeLock = new object();

        SetReady(_context);
    }

    bool IPipeContextHandle<TContext>.IsDisposed => Volatile.Read(ref _disposeStarted) != 0;

    Task<TContext> IPipeContextHandle<TContext>.Context => _context;

    /// <inheritdoc />
    public ValueTask DisposeAsync()
    {
        lock (_disposeLock)
        {
            if (_disposeTask is { } currentAttempt)
            {
                if (!currentAttempt.IsCompleted || currentAttempt.IsCompletedSuccessfully)
                    return new ValueTask(currentAttempt);

                currentAttempt.IgnoreUnobservedExceptions();
            }

            Volatile.Write(ref _disposeStarted, 1);
            _disposeTask = DisposeContextAsync();

            return new ValueTask(_disposeTask);
        }
    }

    /// <inheritdoc />
    protected override async Task StopAgentAsync(StopContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        await DisposeAsync().ConfigureAwait(false);
    }

    async Task DisposeContextAsync()
    {
        TContext context;
        try
        {
            context = await _context.ConfigureAwait(false);
        }
        catch
        {
            // A canceled or faulted creation task never transferred a context to this owner.
            SetCompleted(Task.CompletedTask);
            return;
        }

        await PipeContextDisposer.DisposeAsync(context).ConfigureAwait(false);

        SetCompleted(Task.CompletedTask);
    }

    static async Task<TContext> RequireContextAsync(Task<TContext> context)
    {
        return await context.ConfigureAwait(false)
            ?? throw new InvalidOperationException("The context task completed without a context.");
    }
}
