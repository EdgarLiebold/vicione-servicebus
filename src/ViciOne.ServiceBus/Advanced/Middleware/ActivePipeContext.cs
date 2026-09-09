using System;
using System.Threading.Tasks;

namespace ViciOne.ServiceBus.Advanced.Middleware;

/// <summary>Represents one borrowed use of a pipe context owned by another handle.</summary>
/// <typeparam name="TContext">The pipe context type.</typeparam>
public sealed class ActivePipeContext<TContext> :
    IActivePipeContextHandle<TContext>
    where TContext : class, PipeContext
{
    readonly Task<TContext> _context;
    readonly IPipeContextHandle<TContext> _contextHandle;

    /// <summary>
    /// Creates a borrowed handle backed by the supplied context task and managed by
    /// <paramref name="contextHandle"/>.
    /// </summary>
    /// <param name="contextHandle">The handle that owns the underlying context.</param>
    /// <param name="context">The context task exposed by this active use.</param>
    public ActivePipeContext(IPipeContextHandle<TContext> contextHandle, Task<TContext> context)
    {
        _contextHandle = contextHandle ?? throw new ArgumentNullException(nameof(contextHandle));
        _context = RequireContextAsync(context ?? throw new ArgumentNullException(nameof(context)));
    }

    /// <summary>
    /// Creates a borrowed handle for an already available context managed by
    /// <paramref name="contextHandle"/>.
    /// </summary>
    /// <param name="contextHandle">The handle that owns the underlying context.</param>
    /// <param name="context">The context exposed by this active use.</param>
    public ActivePipeContext(IPipeContextHandle<TContext> contextHandle, TContext context)
    {
        _contextHandle = contextHandle ?? throw new ArgumentNullException(nameof(contextHandle));
        _context = Task.FromResult(context ?? throw new ArgumentNullException(nameof(context)));
    }

    bool IPipeContextHandle<TContext>.IsDisposed => _contextHandle.IsDisposed;

    Task<TContext> IPipeContextHandle<TContext>.Context => _context;

    async Task IActivePipeContextHandle<TContext>.FaultedAsync(Exception exception)
    {
        ArgumentNullException.ThrowIfNull(exception);

        // A fault invalidates the underlying context for all active handles.
        await _contextHandle.DisposeAsync().ConfigureAwait(false);
    }

    /// <summary>Releases this borrowed handle without disposing the shared context.</summary>
    /// <returns>A completed value task.</returns>
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
