using System;
using System.Threading.Tasks;

namespace ViciOne.ServiceBus.Advanced.Middleware;

/// <summary>Represents one borrowed use of a pipe context.</summary>
/// <typeparam name="TContext">The pipe context type.</typeparam>
public interface IActivePipeContextHandle<TContext> :
    IPipeContextHandle<TContext>
    where TContext : class, PipeContext
{
    /// <summary>Reports that this use failed so the owning context can be invalidated.</summary>
    /// <param name="exception">The failure that invalidated the context.</param>
    /// <returns>A task that completes when invalidation has been processed.</returns>
    Task FaultedAsync(Exception exception);
}
