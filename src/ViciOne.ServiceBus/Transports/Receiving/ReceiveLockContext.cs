using System;
using System.Threading.Tasks;

namespace ViciOne.ServiceBus.Transports;

/// <summary>Validates and settles the transport-owned lock for one received delivery.</summary>
public interface ReceiveLockContext
{
    /// <summary>Accepts the delivery and releases its transport lock.</summary>
    /// <param name="cancellationToken">The token that cancels settlement.</param>
    /// <returns>A task that completes after the delivery has been accepted.</returns>
    Task CompleteAsync(CancellationToken cancellationToken = default);

    /// <summary>Releases or rejects the delivery after receive processing fails.</summary>
    /// <param name="exception">The receive-pipeline failure that determines transport settlement.</param>
    /// <param name="cancellationToken">The token that cancels settlement.</param>
    /// <returns>A task that completes after the failed delivery has been settled.</returns>
    Task FaultedAsync(Exception exception, CancellationToken cancellationToken = default);

    /// <summary>Verifies that the transport lock remains valid before pipeline execution.</summary>
    /// <param name="cancellationToken">The token that cancels validation.</param>
    /// <returns>A task that completes when the lock is valid.</returns>
    Task ValidateLockStatusAsync(CancellationToken cancellationToken = default);
}
