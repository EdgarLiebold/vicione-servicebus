using System.Threading;
using System.Threading.Tasks;

namespace ViciOne.ServiceBus.Transports;

/// <summary>Creates provider-specific send contexts and dispatches configured messages through a transport.</summary>
public interface ISendTransport :
    ISendObserverConnector
{
    /// <summary>Creates a provider-specific send context without dispatching the message.</summary>
    /// <typeparam name="T">The outgoing message contract.</typeparam>
    /// <param name="message">The outgoing message.</param>
    /// <param name="pipe">The pipeline that configures the provider-specific send context.</param>
    /// <param name="cancellationToken">The token supplied to context creation and configuration.</param>
    /// <returns>A non-null task that produces a non-null configured send context.</returns>
    Task<SendContext<T>> CreateSendContextAsync<T>(T message, IPipe<SendContext<T>> pipe, CancellationToken cancellationToken)
        where T : class;

    /// <summary>
    /// Sends a message after the transport creates its provider-specific send context and the supplied
    /// pipeline applies delivery settings.
    /// </summary>
    /// <typeparam name="T">The outgoing message contract.</typeparam>
    /// <param name="message">The outgoing message.</param>
    /// <param name="pipe">The pipeline that configures the send context before dispatch.</param>
    /// <param name="cancellationToken">The token that cancels the send operation.</param>
    /// <returns>A non-null task that completes when the transport accepts the send operation; completion does not imply message consumption.</returns>
    Task SendAsync<T>(T message, IPipe<SendContext<T>> pipe, CancellationToken cancellationToken)
        where T : class;
}
