using System;
using System.Threading.Tasks;

namespace ViciOne.ServiceBus.Advanced.Middleware;

/// <summary>Exposes the asynchronous availability and lifetime of a pipe context.</summary>
/// <typeparam name="TContext">The pipe context type.</typeparam>
public interface IPipeContextHandle<TContext> :
    IAsyncDisposable
    where TContext : class, PipeContext
{
    /// <summary>Gets a value indicating whether disposal has begun and the context can no longer be acquired.</summary>
    bool IsDisposed { get; }

    /// <summary>Gets the task that supplies the context.</summary>
    Task<TContext> Context { get; }
}
