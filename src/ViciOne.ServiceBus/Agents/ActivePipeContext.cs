using System;
using System.Threading.Tasks;

namespace ViciOne.ServiceBus.Agents;

/// <summary>
/// An active reference to a pipe context, which is managed by an existing pipe context handle.
/// </summary>
/// <typeparam name="TContext"></typeparam>
public class ActivePipeContext<TContext> :
    ActivePipeContextHandle<TContext>
    where TContext : class, PipeContext
{
    readonly Task<TContext> _context;
    readonly PipeContextHandle<TContext> _contextHandle;

    /// <summary>
    /// Creates an active handle backed by the supplied context task and managed by
    /// <paramref name="contextHandle"/>.
    /// </summary>
    /// <param name="contextHandle">The context handle of the actual context which is being used</param>
    /// <param name="context">The actual context, which should be a completed Task</param>
    public ActivePipeContext(PipeContextHandle<TContext> contextHandle, Task<TContext> context)
    {
        _contextHandle = contextHandle;
        _context = context;
    }

    /// <summary>
    /// Creates an active handle for an already available context managed by
    /// <paramref name="contextHandle"/>.
    /// </summary>
    /// <param name="contextHandle">The context handle of the actual context which is being used</param>
    /// <param name="context">The actual context</param>
    public ActivePipeContext(PipeContextHandle<TContext> contextHandle, TContext context)
    {
        _contextHandle = contextHandle;
        _context = Task.FromResult(context);
    }

    bool PipeContextHandle<TContext>.IsDisposed => _contextHandle.IsDisposed;

    Task<TContext> PipeContextHandle<TContext>.Context => _context;

    async Task ActivePipeContextHandle<TContext>.FaultedAsync(Exception exception, CancellationToken cancellationToken)
    {
        // A fault terminates ownership of the underlying context.
        await _contextHandle.DisposeAsync().ConfigureAwait(false);
    }

    /// <summary>
    /// Releases the resources owned by this instance.
    /// </summary>
    /// <returns>The result of the operation.</returns>
    public ValueTask DisposeAsync()
    {
        // The owning context handle controls the underlying context lifetime.
        return default;
    }
}
