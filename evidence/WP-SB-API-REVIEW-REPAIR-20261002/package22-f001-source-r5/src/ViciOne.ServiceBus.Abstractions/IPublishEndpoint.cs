using System.Threading;
using System.Threading.Tasks;

namespace ViciOne.ServiceBus;

/// <summary>Publishes messages using the topology selected by the configured transport.</summary>
/// <remarks>
/// Completion follows the selected endpoint policy. Buffered sends require explicit <c>IBufferedBus.FlushAsync</c>;
/// <c>UseVolatileOutbox</c> dispatches captured work after successful consumption and discards it on failure.
/// A transactional EF outbox requires <c>IEntityFrameworkTransactionalOutbox.CommitAsync</c>; a caller-owned outer transaction still requires its own commit.
/// See <see href="https://github.com/EdgarLiebold/vicione-servicebus/blob/main/docs/api/completion-boundaries.md">endpoint completion boundaries</see>.
/// </remarks>
public interface IPublishEndpoint :
    IPublishObserverConnector
{
    /// <summary>Publishes a message to all matching subscriptions.</summary>
    /// <typeparam name="TMessage">The message contract type.</typeparam>
    /// <param name="message">The message to publish.</param>
    /// <param name="cancellationToken">The token that cancels publication.</param>
    /// <returns>A task that completes the configured endpoint operation. Buffer or outbox policy may complete it after local capture, before transport dispatch; completion does not confirm delivery or consumption.</returns>
    Task PublishAsync<TMessage>(TMessage message, CancellationToken cancellationToken = default)
        where TMessage : class;

    /// <summary>Publishes a message with application-level metadata.</summary>
    /// <typeparam name="TMessage">The message contract type.</typeparam>
    /// <param name="message">The message to publish.</param>
    /// <param name="options">The application metadata applied to the publication.</param>
    /// <param name="cancellationToken">The token that cancels publication.</param>
    /// <returns>A task that completes the configured endpoint operation. Buffer or outbox policy may complete it after local capture, before transport dispatch; completion does not confirm delivery or consumption.</returns>
    Task PublishAsync<TMessage>(TMessage message, PublishOptions options, CancellationToken cancellationToken = default)
        where TMessage : class;
}
