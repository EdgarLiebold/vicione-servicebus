using System;
using System.Threading.Tasks;

namespace ViciOne.ServiceBus.Agents;

/// <summary>Controls the lifetime of constant pipe context.</summary>
/// <typeparam name="TContext">The pipeline context carried by the member.</typeparam>
public class ConstantPipeContextHandle<TContext> :
    PipeContextHandle<TContext>
    where TContext : class, PipeContext
{
    readonly TContext _context;
    bool _disposed;

    /// <summary>Initializes a new instance.</summary>
    /// <param name="context">The context associated with the operation.</param>
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

    /// <summary>Gets the context.</summary>
    public Task<TContext> Context { get; }
}
