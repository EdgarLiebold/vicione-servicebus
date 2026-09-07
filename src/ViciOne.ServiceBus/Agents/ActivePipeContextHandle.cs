using System;
using System.Threading.Tasks;

namespace ViciOne.ServiceBus.Agents;

/// <summary>An active, in-use reference to a pipe context.</summary>
/// <typeparam name="TContext">The pipeline context carried by the member.</typeparam>
public interface ActivePipeContextHandle<TContext> :
    IPipeContextHandle<TContext>
    where TContext : class, PipeContext
{
    /// <summary>Signals that the active use failed and the underlying context must be invalidated.</summary>
    /// <param name="exception">The failure that invalidated the context.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    Task FaultedAsync(Exception exception);
}
