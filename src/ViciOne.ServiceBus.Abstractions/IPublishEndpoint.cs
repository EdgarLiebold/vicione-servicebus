using System.Threading;
using System.Threading.Tasks;

namespace ViciOne.ServiceBus;

/// <summary>
/// Publishes messages using the topology selected by the configured transport.
/// </summary>
public interface IPublishEndpoint :
    IPublishObserverConnector
{
    /// <summary>Publishes a message to all matching subscriptions.</summary>
    /// <param name="message">The message processed by the operation.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    Task PublishAsync<T>(T message, CancellationToken cancellationToken = default)
        where T : class;

    /// <summary>Publishes a message with application-level metadata.</summary>
    /// <param name="message">The message processed by the operation.</param>
    /// <param name="options">The options used by the operation.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    Task PublishAsync<T>(T message, PublishOptions options, CancellationToken cancellationToken = default)
        where T : class;
}
