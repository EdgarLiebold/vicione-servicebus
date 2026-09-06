using System;
using System.Threading.Tasks;

namespace ViciOne.ServiceBus.Agents;

/// <summary>An active, in-use reference to a pipe context.</summary>
/// <typeparam name="TContext">The pipeline context carried by the member.</typeparam>
public interface ActivePipeContextHandle<TContext> :
    PipeContextHandle<TContext>
    where TContext : class, PipeContext
{
    /// <summary>If the use of this context results in a fault which should cause the context to be disposed, this method signals that behavior to occur.</summary>
    /// <param name="exception">The bad thing that happened.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    Task FaultedAsync(Exception exception, CancellationToken cancellationToken = default);
}
