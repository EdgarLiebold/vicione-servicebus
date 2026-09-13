using System;
using System.Threading.Tasks;
using ViciOne.ServiceBus.Contracts.JobService;

namespace ViciOne.ServiceBus.Advanced;

/// <summary>Publishes lifecycle and progress notifications for an executing job attempt.</summary>
public interface INotifyJobContext
{
    /// <summary>Reports cancellation of the current attempt.</summary>
    /// <param name="cancellationToken">The token that cancels notification publication.</param>
    /// <returns>A task that completes when the cancellation notification has been published.</returns>
    Task NotifyCanceledAsync(CancellationToken cancellationToken = default);
    /// <summary>Reports that the current attempt started.</summary>
    /// <param name="cancellationToken">The token that cancels notification publication.</param>
    /// <returns>A task that completes when the start notification has been published.</returns>
    Task NotifyStartedAsync(CancellationToken cancellationToken = default);
    /// <summary>Reports successful completion of the current attempt.</summary>
    /// <param name="cancellationToken">The token that cancels notification publication.</param>
    /// <returns>A task that completes when the completion notification has been published.</returns>
    Task NotifyCompletedAsync(CancellationToken cancellationToken = default);
    /// <summary>Reports failure of the current attempt and an optional retry delay.</summary>
    /// <param name="exception">The failure reported by the current attempt.</param>
    /// <param name="delay">The optional delay before the coordinator may start another attempt.</param>
    /// <param name="cancellationToken">The token that cancels notification publication.</param>
    /// <returns>A task that completes when the fault notification has been published.</returns>
    Task NotifyFaultedAsync(Exception exception, TimeSpan? delay = default, CancellationToken cancellationToken = default);
    /// <summary>Publishes a progress snapshot for the current attempt.</summary>
    /// <param name="progress">The progress snapshot to publish.</param>
    /// <param name="cancellationToken">The token that cancels notification publication.</param>
    /// <returns>A task that completes when the progress notification has been published.</returns>
    Task NotifyProgressAsync(ISetJobProgress progress, CancellationToken cancellationToken = default);
}
