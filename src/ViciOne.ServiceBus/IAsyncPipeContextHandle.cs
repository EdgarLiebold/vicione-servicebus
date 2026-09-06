using System;
using System.Threading.Tasks;

namespace ViciOne.ServiceBus.Advanced;

/// <summary>Supports the asynchronous notification of a PipeContext becoming available (this is a future of a future, basically).</summary>
/// <typeparam name="TContext">The context type.</typeparam>
public interface IAsyncPipeContextHandle<TContext> :
    PipeContextHandle<TContext>
    where TContext : class, PipeContext
{
    /// <summary>Called when the PipeContext has been created and is available for use.</summary>
    /// <param name="context">The context associated with the operation.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    Task CreatedAsync(TContext context, CancellationToken cancellationToken = default);

    /// <summary>Called when the PipeContext creation was canceled.</summary>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    Task CreateCanceledAsync(CancellationToken cancellationToken = default);

    /// <summary>Called when the PipeContext creation failed.</summary>
    /// <param name="exception">The exception associated with the operation.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    Task CreateFaultedAsync(Exception exception, CancellationToken cancellationToken = default);

    /// <summary>
    /// Called when the successfully created PipeContext becomes faulted, indicating that it
    /// should no longer be used.
    /// </summary>
    /// <param name="exception">The exception which occurred.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    Task FaultedAsync(Exception exception, CancellationToken cancellationToken = default);
}
