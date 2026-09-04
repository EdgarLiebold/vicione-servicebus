using System.Threading;
using System.Threading.Tasks;

namespace ViciOne.ServiceBus.Transports;

/// <summary>
/// Defines the contract for send transport.
/// </summary>
public interface ISendTransport :
    ISendObserverConnector
{
    /// <summary>
    /// Creates send context.
    /// </summary>
    /// <typeparam name="T">The t type.</typeparam>
    /// <param name="message">The message value.</param>
    /// <param name="pipe">The pipe value.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The result of the operation.</returns>
    Task<SendContext<T>> CreateSendContextAsync<T>(T message, IPipe<SendContext<T>> pipe, CancellationToken cancellationToken)
        where T : class;

    /// <summary>
    /// Send a message to the transport. The transport creates the OldSendContext, and calls back to
    /// allow the context to be modified to customize the message delivery.
    /// The transport specifies the defaults for the message as configured, and then allows the
    /// caller to modify the send context to include the required settings (durable, mandatory, etc.).
    /// </summary>
    /// <typeparam name="T">The message type</typeparam>
    /// <param name="message"></param>
    /// <param name="pipe">The pipe invoked when sending a message, to do extra stuff</param>
    /// <param name="cancellationToken">Cancel the send operation (if possible)</param>
    /// <returns>The task which is completed once the Send is acknowledged by the broker</returns>
    Task SendAsync<T>(T message, IPipe<SendContext<T>> pipe, CancellationToken cancellationToken)
        where T : class;
}
