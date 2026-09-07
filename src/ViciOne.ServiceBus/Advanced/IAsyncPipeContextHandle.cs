using System;
using System.Threading.Tasks;

namespace ViciOne.ServiceBus.Advanced;

/// <summary>Observes the lifecycle of a pipe context that is created asynchronously.</summary>
/// <typeparam name="TContext">The context type.</typeparam>
public interface IAsyncPipeContextHandle<TContext> :
    IPipeContextHandle<TContext>
    where TContext : class, PipeContext
{
    /// <summary>Signals that the pipe context is ready for use.</summary>
    /// <param name="context">The created pipe context.</param>
    /// <returns>A task that completes when the created state has been published.</returns>
    Task CreatedAsync(TContext context);

    /// <summary>Signals that pipe-context creation was canceled.</summary>
    /// <param name="cancellationToken">The token associated with the creation cancellation.</param>
    /// <returns>A task that completes when cancellation has been published.</returns>
    Task CreateCanceledAsync(CancellationToken cancellationToken);

    /// <summary>Signals that pipe-context creation failed.</summary>
    /// <param name="exception">The creation failure.</param>
    /// <returns>A task that completes when the creation failure has been published.</returns>
    Task CreateFaultedAsync(Exception exception);

    /// <summary>
    /// Signals that a previously created pipe context faulted and can no longer be used.
    /// </summary>
    /// <param name="exception">The context failure.</param>
    /// <returns>A task that completes when the context failure has been published.</returns>
    Task FaultedAsync(Exception exception);
}
