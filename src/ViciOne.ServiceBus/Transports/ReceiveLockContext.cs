using System;
using System.Threading.Tasks;

namespace ViciOne.ServiceBus.Transports;

/// <summary>
/// Encapsulates a transport lock
/// </summary>
public interface ReceiveLockContext
{
    /// <summary>
    /// Called to complete the message
    /// </summary>
    /// <returns></returns>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    Task CompleteAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Called if the message was faulted. This method should NOT throw an exception.
    /// </summary>
    /// <param name="exception"></param>
    /// <returns></returns>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    Task FaultedAsync(Exception exception, CancellationToken cancellationToken = default);

    /// <summary>
    /// Validate that the lock is still valid
    /// </summary>
    /// <returns></returns>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    Task ValidateLockStatusAsync(CancellationToken cancellationToken = default);
}
