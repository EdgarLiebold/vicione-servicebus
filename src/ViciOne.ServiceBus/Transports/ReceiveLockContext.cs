using System;
using System.Threading.Tasks;

namespace ViciOne.ServiceBus.Transports;

/// <summary>Encapsulates a transport lock.</summary>
public interface ReceiveLockContext
{
    /// <summary>Called to complete the message.</summary>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    Task CompleteAsync(CancellationToken cancellationToken = default);

    /// <summary>Called if the message was faulted. This method should NOT throw an exception.</summary>
    /// <param name="exception">The exception associated with the operation.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    Task FaultedAsync(Exception exception, CancellationToken cancellationToken = default);

    /// <summary>Validate that the lock is still valid.</summary>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    Task ValidateLockStatusAsync(CancellationToken cancellationToken = default);
}
