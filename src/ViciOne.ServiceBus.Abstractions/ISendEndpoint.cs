using System.Threading;
using System.Threading.Tasks;

namespace ViciOne.ServiceBus;

/// <summary>Sends messages to a specific destination using application-level options.</summary>
public interface ISendEndpoint :
    ISendObserverConnector
{
    /// <summary>Sends a message to the endpoint.</summary>
    /// <typeparam name="TMessage">The message contract type.</typeparam>
    /// <param name="message">The message to send.</param>
    /// <param name="cancellationToken">The token that cancels the send operation.</param>
    /// <returns>A task that completes when the destination transport has accepted the message.</returns>
    Task SendAsync<TMessage>(TMessage message, CancellationToken cancellationToken = default)
        where TMessage : class;

    /// <summary>Sends a message to the endpoint with application-level metadata.</summary>
    /// <typeparam name="TMessage">The message contract type.</typeparam>
    /// <param name="message">The message to send.</param>
    /// <param name="options">The application metadata applied to the outgoing message.</param>
    /// <param name="cancellationToken">The token that cancels the send operation.</param>
    /// <returns>A task that completes when the destination transport has accepted the message.</returns>
    Task SendAsync<TMessage>(TMessage message, SendOptions options, CancellationToken cancellationToken = default)
        where TMessage : class;
}
