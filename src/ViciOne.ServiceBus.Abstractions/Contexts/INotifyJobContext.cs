using System;
using System.Threading.Tasks;
using ViciOne.ServiceBus.Contracts.JobService;

namespace ViciOne.ServiceBus.Advanced;

/// <summary>Exposes state for notify job operations.</summary>
public interface INotifyJobContext
{
    /// <summary>Notifies registered observers about canceled.</summary>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    Task NotifyCanceledAsync(CancellationToken cancellationToken = default);
    /// <summary>Notifies registered observers about started.</summary>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    Task NotifyStartedAsync(CancellationToken cancellationToken = default);
    /// <summary>Reports that notify has completed.</summary>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    Task NotifyCompletedAsync(CancellationToken cancellationToken = default);
    /// <summary>Reports that notify has faulted.</summary>
    /// <param name="exception">The exception associated with the operation.</param>
    /// <param name="delay">The delay before the operation is attempted.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    Task NotifyFaultedAsync(Exception exception, TimeSpan? delay = default, CancellationToken cancellationToken = default);
    /// <summary>Notifies registered observers about job progress.</summary>
    /// <param name="progress">The progress.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    Task NotifyJobProgressAsync(SetJobProgress progress, CancellationToken cancellationToken = default);
}
