using System;
using System.Threading.Tasks;

namespace ViciOne.ServiceBus.Advanced;

/// <summary>
/// Owns the asynchronous availability and lifetime of a pipe context.
/// </summary>
/// <typeparam name="TContext">The pipeline context carried by the member.</typeparam>
public interface PipeContextHandle<TContext> :
    IAsyncDisposable
    where TContext : class, PipeContext
{
    /// <summary>Gets whether the context has been disposed and can no longer be used.</summary>
    bool IsDisposed { get; }

    /// <summary>Gets the task that completes with the created context.</summary>
    Task<TContext> Context { get; }
}
