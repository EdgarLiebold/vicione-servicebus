using System;
using System.Threading.Tasks;

namespace ViciOne.ServiceBus.JobService;

/// <summary>Owns and controls one job executing in the local service instance.</summary>
internal interface JobHandle :
    IAsyncDisposable
{
    /// <summary>Gets the stable identifier of the executing job.</summary>
    Guid JobId { get; }

    /// <summary>Gets the identifier of the execution attempt owned by this handle.</summary>
    Guid AttemptId { get; }

    /// <summary>Gets the underlying consumer-pipeline task.</summary>
    Task Execution { get; }

    /// <summary>Gets the local ownership task, which also completes after an execution is abandoned at its cancellation deadline.</summary>
    Task Completion { get; }

    /// <summary>Requests cancellation and waits for the consumer to stop within its configured grace period.</summary>
    /// <param name="reason">The optional cancellation reason.</param>
    /// <param name="cancellationToken">The token that cancels waiting for the consumer to stop.</param>
    /// <returns>Completion indicates that the consumer stopped or the handle relinquished local ownership at the grace-period deadline.</returns>
    Task CancelAsync(string? reason, CancellationToken cancellationToken = default);
}
