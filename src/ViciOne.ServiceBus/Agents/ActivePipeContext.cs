using System;
using System.Threading.Tasks;

namespace ViciOne.ServiceBus.Agents;

/// <summary>An active reference to a pipe context, which is managed by an existing pipe context handle.</summary>
/// <typeparam name="TContext">The pipeline context carried by the member.</typeparam>
public sealed class ActivePipeContext<TContext> :
    ActivePipeContextHandle<TContext>
    where TContext : class, PipeContext
{
    readonly Task<TContext> _context;
    readonly PipeContextHandle<TContext> _contextHandle;

    /// <summary>
    /// Creates an active handle backed by the supplied context task and managed by
    /// <paramref name="contextHandle"/>.
    /// </summary>
    /// <param name="contextHandle">The handle that owns the underlying context.</param>
    /// <param name="context">The context task exposed by this active use.</param>
    public ActivePipeContext(PipeContextHandle<TContext> contextHandle, Task<TContext> context)
    {
        _contextHandle = contextHandle ?? throw new ArgumentNullException(nameof(contextHandle));
        _context = RequireContextAsync(context ?? throw new ArgumentNullException(nameof(context)));
    }

    /// <summary>
    /// Creates an active handle for an already available context managed by
    /// <paramref name="contextHandle"/>.
    /// </summary>
    /// <param name="contextHandle">The handle that owns the underlying context.</param>
    /// <param name="context">The context exposed by this active use.</param>
    public ActivePipeContext(PipeContextHandle<TContext> contextHandle, TContext context)
    {
        _contextHandle = contextHandle ?? throw new ArgumentNullException(nameof(contextHandle));
        _context = Task.FromResult(context ?? throw new ArgumentNullException(nameof(context)));
    }

    bool PipeContextHandle<TContext>.IsDisposed => _contextHandle.IsDisposed;

    Task<TContext> PipeContextHandle<TContext>.Context => _context;

    async Task ActivePipeContextHandle<TContext>.FaultedAsync(Exception exception)
    {
        ArgumentNullException.ThrowIfNull(exception);

        // A fault invalidates the underlying context for all active handles.
        await _contextHandle.DisposeAsync().ConfigureAwait(false);
    }

    /// <summary>Completes disposal of this borrowed handle without disposing the shared context.</summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public ValueTask DisposeAsync()
    {
        // The owning context handle controls the underlying context lifetime.
        return default;
    }

    static async Task<TContext> RequireContextAsync(Task<TContext> context)
    {
        return await context.ConfigureAwait(false)
            ?? throw new InvalidOperationException("The active context task completed without a context.");
    }
}
