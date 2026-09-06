using System;
using System.Threading;
using System.Threading.Tasks;
using ViciOne.ServiceBus.Middleware;

namespace ViciOne.ServiceBus.Agents;

/// <summary>An Agent Provocateur that uses a context handle for the activate state of the agent.</summary>
/// <typeparam name="TContext">The pipeline context carried by the member.</typeparam>
public class ActivePipeContextAgent<TContext> :
    Agent,
    IActivePipeContextAgent<TContext>
    where TContext : class, PipeContext
{
    static readonly string _caption = $"Active<{typeof(TContext).Name}>";

    readonly ActivePipeContextHandle<TContext> _contextHandle;

    /// <summary>Initializes a new instance.</summary>
    /// <param name="context">The context associated with the operation.</param>
    public ActivePipeContextAgent(ActivePipeContextHandle<TContext> context)
    {
        _contextHandle = context;

        context.Context.ContinueWith(SetReady, CancellationToken.None, TaskContinuationOptions.OnlyOnRanToCompletion, TaskScheduler.Default);
        context.Context.ContinueWith(SetFaulted, CancellationToken.None, TaskContinuationOptions.NotOnRanToCompletion, TaskScheduler.Default);
    }

    bool PipeContextHandle<TContext>.IsDisposed => _contextHandle.IsDisposed;

    Task<TContext> PipeContextHandle<TContext>.Context => _contextHandle.Context;

    Task ActivePipeContextHandle<TContext>.FaultedAsync(Exception exception, CancellationToken cancellationToken)
    {
        return _contextHandle.FaultedAsync(exception, cancellationToken: cancellationToken);
    }

    ValueTask IAsyncDisposable.DisposeAsync()
    {
        return _contextHandle.DisposeAsync();
    }

    /// <inheritdoc />
    protected override async Task StopAgentAsync(StopContext context)
    {
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
