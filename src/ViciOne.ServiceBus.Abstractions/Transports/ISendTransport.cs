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
    /// Sends a message after the transport creates its provider-specific send context and the supplied
    /// pipeline applies delivery settings.
    /// </summary>
    /// <typeparam name="T">The message type.</typeparam>
    /// <param name="message">The message to process.</param>
    /// <param name="pipe">The pipeline that configures the send context before dispatch.</param>
    /// <param name="cancellationToken">The token that cancels the send operation.</param>
    /// <returns>A task that completes when the configured transport has accepted the send operation; completion does not imply message consumption.</returns>
    Task SendAsync<T>(T message, IPipe<SendContext<T>> pipe, CancellationToken cancellationToken)
        where T : class;
}
