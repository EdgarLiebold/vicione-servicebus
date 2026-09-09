using System;
using System.Threading.Tasks;

namespace ViciOne.ServiceBus.Advanced.Middleware;

/// <summary>Receives creation and runtime outcomes for an asynchronously supplied pipe context.</summary>
/// <typeparam name="TContext">The pipe context type.</typeparam>
public interface IAsyncPipeContextHandle<TContext> :
    IPipeContextHandle<TContext>
    where TContext : class, PipeContext
{
    /// <summary>Reports that the pipe context is ready for use.</summary>
    /// <param name="context">
    /// The created pipe context being handed off. The handle disposes the context if an earlier
    /// terminal creation outcome prevents the handoff.
    /// </param>
    /// <returns>A task that completes after the context is published or a rejected context is disposed.</returns>
    Task CreatedAsync(TContext context);

    /// <summary>Reports that pipe-context creation was canceled.</summary>
    /// <param name="cancellationToken">The token associated with the creation cancellation.</param>
    /// <returns>A task that completes when cancellation has been published.</returns>
    Task CreateCanceledAsync(CancellationToken cancellationToken);

    /// <summary>Reports that pipe-context creation failed.</summary>
    /// <param name="exception">The creation failure.</param>
    /// <returns>A task that completes when the creation failure has been published.</returns>
    Task CreateFaultedAsync(Exception exception);

    /// <summary>
    /// Reports that a previously created pipe context failed and can no longer be used.
    /// </summary>
    /// <param name="exception">The context failure.</param>
    /// <returns>A task that completes when the context failure has been published.</returns>
    Task FaultedAsync(Exception exception);
}
