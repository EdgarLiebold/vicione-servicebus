using System;
using System.Threading.Tasks;

namespace ViciOne.ServiceBus.Agents;

/// <summary>
/// Provides a constant pipe context handle implementation.
/// </summary>
/// <typeparam name="TContext">The t context type.</typeparam>
public class ConstantPipeContextHandle<TContext> :
    PipeContextHandle<TContext>
    where TContext : class, PipeContext
{
    readonly TContext _context;
    bool _disposed;

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="context">The operation context.</param>
    public ConstantPipeContextHandle(TContext context)
    {
        _context = context;

        Context = Task.FromResult(context);
    }

    async ValueTask IAsyncDisposable.DisposeAsync()
    {
        if (_disposed)
            return;

        switch (_context)
        {
            case IAsyncDisposable asyncDisposable:
                await asyncDisposable.DisposeAsync().ConfigureAwait(false);
                break;
            case IDisposable disposable:
                disposable.Dispose();
                break;
        }

        _disposed = true;
    }

    bool PipeContextHandle<TContext>.IsDisposed => _disposed;

    /// <summary>
    /// Gets the context value.
    /// </summary>
    public Task<TContext> Context { get; }
}
