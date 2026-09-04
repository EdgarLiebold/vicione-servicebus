using System;
using System.Threading.Tasks;
using ViciOne.ServiceBus.Contracts.JobService;

namespace ViciOne.ServiceBus.Advanced;

/// <summary>
/// Defines the contract for notify job context.
/// </summary>
public interface INotifyJobContext
{
    /// <summary>
    /// Performs the notify canceled operation.
    /// </summary>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The result of the operation.</returns>
    Task NotifyCanceledAsync(CancellationToken cancellationToken = default);
    /// <summary>
    /// Performs the notify started operation.
    /// </summary>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The result of the operation.</returns>
    Task NotifyStartedAsync(CancellationToken cancellationToken = default);
    /// <summary>
    /// Performs the notify completed operation.
    /// </summary>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The result of the operation.</returns>
    Task NotifyCompletedAsync(CancellationToken cancellationToken = default);
    /// <summary>
    /// Performs the notify faulted operation.
    /// </summary>
    /// <param name="exception">The exception associated with the operation.</param>
    /// <param name="delay">The delay value.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The result of the operation.</returns>
    Task NotifyFaultedAsync(Exception exception, TimeSpan? delay = default, CancellationToken cancellationToken = default);
    /// <summary>
    /// Performs the notify job progress operation.
    /// </summary>
    /// <param name="progress">The progress value.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The result of the operation.</returns>
    Task NotifyJobProgressAsync(SetJobProgress progress, CancellationToken cancellationToken = default);
}
