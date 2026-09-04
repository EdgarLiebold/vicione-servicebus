using System;
using System.Threading;
using System.Threading.Tasks;

namespace ViciOne.ServiceBus.Clients;

/// <summary>
/// Defines the contract for handler connect handle.
/// </summary>
/// <typeparam name="T">The t type.</typeparam>
public interface HandlerConnectHandle<T> :
    HandlerConnectHandle
    where T : class
{
    /// <summary>
    /// Gets the task value.
    /// </summary>
    Task<Response<T>> Task { get; }
}


/// <summary>
/// Defines the contract for handler connect handle.
/// </summary>
public interface HandlerConnectHandle :
    ConnectHandle
{
    /// <summary>
    /// Performs the try set exception operation.
    /// </summary>
    /// <param name="exception">The exception associated with the operation.</param>
    void TrySetException(Exception exception);

    /// <summary>
    /// Performs the try set canceled operation.
    /// </summary>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    void TrySetCanceled(CancellationToken cancellationToken);
}
