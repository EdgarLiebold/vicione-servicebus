using System;
using System.Threading.Tasks;

namespace ViciOne.ServiceBus.SqlTransport;

/// <summary>
/// Defines the contract for message lock context.
/// </summary>
public interface MessageLockContext
{
    /// <summary>
    /// Performs the complete operation.
    /// </summary>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The result of the operation.</returns>
    Task CompleteAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Performs the abandon operation.
    /// </summary>
    /// <param name="exception">The exception associated with the operation.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The result of the operation.</returns>
    Task AbandonAsync(Exception exception, CancellationToken cancellationToken = default);
    /// <summary>
    /// Performs the dead letter operation.
    /// </summary>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The result of the operation.</returns>
    Task DeadLetterAsync(CancellationToken cancellationToken = default);
    /// <summary>
    /// Performs the dead letter operation.
    /// </summary>
    /// <param name="exception">The exception associated with the operation.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The result of the operation.</returns>
    Task DeadLetterAsync(Exception exception, CancellationToken cancellationToken = default);
}
