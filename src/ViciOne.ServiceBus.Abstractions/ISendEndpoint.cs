using System.Threading;
using System.Threading.Tasks;

namespace ViciOne.ServiceBus;

/// <summary>
/// Sends messages to a specific destination using application-level options.
/// </summary>
public interface ISendEndpoint :
    ISendObserverConnector
{
    /// <summary>Sends a message to the endpoint.</summary>
    /// <param name="message">The message processed by the operation.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    Task SendAsync<T>(T message, CancellationToken cancellationToken = default)
        where T : class;

    /// <summary>Sends a message to the endpoint with application-level metadata.</summary>
    /// <param name="message">The message processed by the operation.</param>
    /// <param name="options">The options used by the operation.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    Task SendAsync<T>(T message, SendOptions options, CancellationToken cancellationToken = default)
        where T : class;
}
