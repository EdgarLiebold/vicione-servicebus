using System;
using System.Threading.Tasks;
using ViciOne.ServiceBus.Middleware;
using ViciOne.ServiceBus.Util;

namespace ViciOne.ServiceBus.Agents;

/// <summary>
/// A PipeContext, which as an agent can be Stopped, which disposes of the context making it unavailable
/// </summary>
/// <typeparam name="TContext"></typeparam>
public class PipeContextAgent<TContext> :
    Agent,
    IPipeContextAgent<TContext>
    where TContext : class, PipeContext
{
    readonly Task<TContext> _context;
    readonly TaskCompletionSource<DateTime> _inactive;
    readonly TimeProvider _timeProvider;

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="context">The operation context.</param>
    public PipeContextAgent(TContext context)
        : this(Task.FromResult(context), context.GetTimeProvider())
    {
    }

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="context">The operation context.</param>
    /// <param name="timeProvider">The time provider value.</param>
    public PipeContextAgent(Task<TContext> context, TimeProvider? timeProvider = null)
    {
        _context = context;
        _inactive = TaskCompletionSources.Create<DateTime>();
        _timeProvider = timeProvider ?? TimeProvider.System;

        SetReady(_context);
    }

    bool PipeContextHandle<TContext>.IsDisposed => _inactive.Task.IsCompleted;

    Task<TContext> PipeContextHandle<TContext>.Context => _context;

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        // dispose only once
        if (!_inactive.TrySetResult(_timeProvider.GetUtcNow().UtcDateTime))
            return;

        if (_context.Status == TaskStatus.RanToCompletion)
        {
            switch (_context.Result)
            {
                case IAsyncDisposable asyncDisposable:
                    await asyncDisposable.DisposeAsync().ConfigureAwait(false);
                    break;
                case IDisposable disposable:
                    disposable.Dispose();
                    break;
            }
        }

        SetCompleted(_inactive.Task);
    }

    /// <inheritdoc />
    protected override async Task StopAgentAsync(StopContext context)
    {
        await DisposeAsync().ConfigureAwait(false);
    }
}
