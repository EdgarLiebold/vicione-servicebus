using System.Threading;
using System.Threading.Tasks;

namespace ViciOne.ServiceBus.Transports;

/// <summary>Defines the operations required by send transport.</summary>
public interface ISendTransport :
    ISendObserverConnector
{
    /// <summary>Creates send context.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="message">The message to process.</param>
    /// <param name="pipe">The pipeline stages to apply.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that produces the created value.</returns>
    Task<SendContext<T>> CreateSendContextAsync<T>(T message, IPipe<SendContext<T>> pipe, CancellationToken cancellationToken)
        where T : class;

    /// <summary>
    /// Send a message to the transport. The transport creates the OldSendContext, and calls back to
    /// allow the context to be modified to customize the message delivery.
    /// The transport specifies the defaults for the message as configured, and then allows the
    /// caller to modify the send context to include the required settings (durable, mandatory, etc.).
    /// </summary>
    /// <typeparam name="T">The message type.</typeparam>
    /// <param name="message">The message to process.</param>
    /// <param name="pipe">The pipe invoked when sending a message, to do extra stuff.</param>
    /// <param name="cancellationToken">Cancel the send operation (if possible).</param>
    /// <returns>A task that completes when the configured transport has accepted the send operation; completion does not imply message consumption.</returns>
    Task SendAsync<T>(T message, IPipe<SendContext<T>> pipe, CancellationToken cancellationToken)
        where T : class;
}
