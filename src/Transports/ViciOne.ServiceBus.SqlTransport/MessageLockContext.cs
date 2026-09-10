using System;
using System.Threading.Tasks;

namespace ViciOne.ServiceBus.SqlTransport;

/// <summary>Exposes state for message lock operations.</summary>
public interface MessageLockContext
{
    /// <summary>Marks the current operation as complete.</summary>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    Task CompleteAsync(CancellationToken cancellationToken = default);

    /// <summary>Abandons the current message or operation.</summary>
    /// <param name="exception">The exception associated with the operation.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    Task AbandonAsync(Exception exception, CancellationToken cancellationToken = default);
    /// <summary>Moves the current message to the dead-letter destination.</summary>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    Task DeadLetterAsync(CancellationToken cancellationToken = default);
    /// <summary>Moves the current message to the dead-letter destination.</summary>
    /// <param name="exception">The exception associated with the operation.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    Task DeadLetterAsync(Exception exception, CancellationToken cancellationToken = default);
}
