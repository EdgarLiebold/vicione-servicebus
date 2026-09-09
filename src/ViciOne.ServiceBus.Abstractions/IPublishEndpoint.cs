using System.Threading;
using System.Threading.Tasks;

namespace ViciOne.ServiceBus;

/// <summary>Publishes messages using the topology selected by the configured transport.</summary>
public interface IPublishEndpoint :
    IPublishObserverConnector
{
    /// <summary>Publishes a message to all matching subscriptions.</summary>
    /// <typeparam name="TMessage">The message contract type.</typeparam>
    /// <param name="message">The message to publish.</param>
    /// <param name="cancellationToken">The token that cancels publication.</param>
    /// <returns>A task that completes when the transport has accepted the publication.</returns>
    Task PublishAsync<TMessage>(TMessage message, CancellationToken cancellationToken = default)
        where TMessage : class;

    /// <summary>Publishes a message with application-level metadata.</summary>
    /// <typeparam name="TMessage">The message contract type.</typeparam>
    /// <param name="message">The message to publish.</param>
    /// <param name="options">The application metadata applied to the publication.</param>
    /// <param name="cancellationToken">The token that cancels publication.</param>
    /// <returns>A task that completes when the transport has accepted the publication.</returns>
    Task PublishAsync<TMessage>(TMessage message, PublishOptions options, CancellationToken cancellationToken = default)
        where TMessage : class;
}
