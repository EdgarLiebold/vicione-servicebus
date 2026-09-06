using System;
using System.Threading.Tasks;

namespace ViciOne.ServiceBus.Advanced;

/// <summary>Observes the lifecycle of a pipe context that is created asynchronously.</summary>
/// <typeparam name="TContext">The context type.</typeparam>
public interface IAsyncPipeContextHandle<TContext> :
    PipeContextHandle<TContext>
    where TContext : class, PipeContext
{
    /// <summary>Signals that the pipe context is ready for use.</summary>
    /// <param name="context">The created pipe context.</param>
    /// <param name="cancellationToken">Cancels the notification.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    Task CreatedAsync(TContext context, CancellationToken cancellationToken = default);

    /// <summary>Signals that pipe-context creation was canceled.</summary>
    /// <param name="cancellationToken">Cancels the notification.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    Task CreateCanceledAsync(CancellationToken cancellationToken = default);

    /// <summary>Signals that pipe-context creation failed.</summary>
    /// <param name="exception">The creation failure.</param>
    /// <param name="cancellationToken">Cancels the notification.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    Task CreateFaultedAsync(Exception exception, CancellationToken cancellationToken = default);

    /// <summary>
    /// Signals that a previously created pipe context faulted and can no longer be used.
    /// </summary>
    /// <param name="exception">The context failure.</param>
    /// <param name="cancellationToken">Cancels the notification.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    Task FaultedAsync(Exception exception, CancellationToken cancellationToken = default);
}
