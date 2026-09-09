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

    ValueTask IAsyncDisposable.DisposeAsync()
    {
        return _contextHandle.DisposeAsync();
    }

    /// <inheritdoc />
    protected override async Task StopAgentAsync(StopContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

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

    /// <inheritdoc />
    public override string ToString()
    {
        return _caption;
    }
}
