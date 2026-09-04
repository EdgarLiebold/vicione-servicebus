using System;
using System.Threading;
using System.Threading.Tasks;

namespace ViciOne.ServiceBus.Advanced;

/// <summary>
/// Defines the contract for request send endpoint.
/// </summary>
/// <typeparam name="T">The t type.</typeparam>
public interface IRequestSendEndpoint<T>
    where T : class
{
    /// <summary>
    /// Sends a message to the configured destination.
    /// </summary>
    /// <param name="requestId">The request id value.</param>
    /// <param name="values">The values value.</param>
    /// <param name="pipe">The pipe value.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The result of the operation.</returns>
    Task<T> SendAsync(Guid requestId, object values, IPipe<SendContext<T>> pipe, CancellationToken cancellationToken);

    /// <summary>
    /// Sends a message to the configured destination.
    /// </summary>
    /// <param name="requestId">The request id value.</param>
    /// <param name="message">The message value.</param>
    /// <param name="pipe">The pipe value.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The result of the operation.</returns>
    Task SendAsync(Guid requestId, T message, IPipe<SendContext<T>> pipe, CancellationToken cancellationToken);
}
