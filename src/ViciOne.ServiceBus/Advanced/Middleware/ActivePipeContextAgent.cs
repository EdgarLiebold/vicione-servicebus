using System;
using System.Threading;
using System.Threading.Tasks;

namespace ViciOne.ServiceBus.Advanced.Middleware;

/// <summary>Tracks one borrowed pipe-context use as a supervised lifecycle.</summary>
/// <typeparam name="TContext">The pipe context type.</typeparam>
public sealed class ActivePipeContextAgent<TContext> :
    Agent,
    IActivePipeContextAgent<TContext>
    where TContext : class, PipeContext
{
    static readonly string _caption = $"Active<{typeof(TContext).Name}>";

    readonly IActivePipeContextHandle<TContext> _contextHandle;
    readonly object _useLock = new();
    TaskCompletionSource? _usesCompleted;
    int _activeUses;
    bool _closing;

    /// <summary>Initializes an agent that owns the supplied borrowed handle.</summary>
    /// <param name="context">The borrowed context handle to track and release.</param>
    public ActivePipeContextAgent(IActivePipeContextHandle<TContext> context)
    {
        _contextHandle = context ?? throw new ArgumentNullException(nameof(context));

        context.Context.ContinueWith(SetReady, CancellationToken.None, TaskContinuationOptions.OnlyOnRanToCompletion, TaskScheduler.Default);
        context.Context.ContinueWith(SetFaulted, CancellationToken.None, TaskContinuationOptions.NotOnRanToCompletion, TaskScheduler.Default);
    }

    bool IPipeContextHandle<TContext>.IsDisposed => _contextHandle.IsDisposed;

    Task<TContext> IPipeContextHandle<TContext>.Context => _contextHandle.Context;

    Task IActivePipeContextHandle<TContext>.FaultedAsync(Exception exception)
    {
        ArgumentNullException.ThrowIfNull(exception);

        return _contextHandle.FaultedAsync(exception);
    }

    async ValueTask IAsyncDisposable.DisposeAsync()
    {
        await WaitForUsesAsync().ConfigureAwait(false);
        await _contextHandle.DisposeAsync().ConfigureAwait(false);
    }

    /// <summary>Protects one pipe operation from concurrent agent shutdown.</summary>
    /// <returns>A lease to release when the pipe operation has completed.</returns>
    public IDisposable BeginUse()
    {
        lock (_useLock)
        {
            if (_closing)
                throw new InvalidOperationException("The active context is stopping.");

            _activeUses++;
            return new UseLease(this);
        }
    }

    /// <inheritdoc />
    protected override async Task StopAgentAsync(StopContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        await WaitForUsesAsync().WaitAsync(context.CancellationToken).ConfigureAwait(false);

        try
        {
            await _contextHandle.DisposeAsync().ConfigureAwait(false);
        }
        finally
        {
            SetCompleted(Task.CompletedTask);
            await Completed.ConfigureAwait(false);
        }
    }

    Task WaitForUsesAsync()
    {
        lock (_useLock)
        {
            _closing = true;
            return _activeUses == 0
                ? Task.CompletedTask
                : (_usesCompleted ??= new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously)).Task;
        }
    }

    sealed class UseLease(ActivePipeContextAgent<TContext> owner) : IDisposable
    {
        ActivePipeContextAgent<TContext>? _owner = owner;

        public void Dispose()
        {
            ActivePipeContextAgent<TContext>? owner = Interlocked.Exchange(ref _owner, null);
            if (owner is null)
                return;

            lock (owner._useLock)
            {
                if (--owner._activeUses == 0)
                    owner._usesCompleted?.TrySetResult();
            }
        }
    }

    /// <inheritdoc />
    public override string ToString()
    {
        return _caption;
    }
}
