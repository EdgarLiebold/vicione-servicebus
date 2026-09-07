using System;
using System.Threading;
using System.Threading.Tasks;
using ViciOne.ServiceBus.Middleware;

namespace ViciOne.ServiceBus.Agents;

/// <summary>Tracks one active use of a pipe context as a supervised agent.</summary>
/// <typeparam name="TContext">The pipeline context carried by the member.</typeparam>
public sealed class ActivePipeContextAgent<TContext> :
    Agent,
    IActivePipeContextAgent<TContext>
    where TContext : class, PipeContext
{
    static readonly string _caption = $"Active<{typeof(TContext).Name}>";

    readonly ActivePipeContextHandle<TContext> _contextHandle;

    /// <summary>Initializes a new instance.</summary>
    /// <param name="context">The active context handle tracked by the agent.</param>
    public ActivePipeContextAgent(ActivePipeContextHandle<TContext> context)
    {
        _contextHandle = context ?? throw new ArgumentNullException(nameof(context));

        context.Context.ContinueWith(SetReady, CancellationToken.None, TaskContinuationOptions.OnlyOnRanToCompletion, TaskScheduler.Default);
        context.Context.ContinueWith(SetFaulted, CancellationToken.None, TaskContinuationOptions.NotOnRanToCompletion, TaskScheduler.Default);
    }

    bool IPipeContextHandle<TContext>.IsDisposed => _contextHandle.IsDisposed;

    Task<TContext> IPipeContextHandle<TContext>.Context => _contextHandle.Context;

    Task ActivePipeContextHandle<TContext>.FaultedAsync(Exception exception)
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

        if (_contextHandle.Context.Status == TaskStatus.RanToCompletion)
            await _contextHandle.DisposeAsync().ConfigureAwait(false);

        SetCompleted(Task.CompletedTask);
    }

    /// <inheritdoc />
    public override string ToString()
    {
        return _caption;
    }
}
