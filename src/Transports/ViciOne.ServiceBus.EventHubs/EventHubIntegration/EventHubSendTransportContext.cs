using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using ViciOne.ServiceBus.Transports;

namespace ViciOne.ServiceBus.EventHubs;

/// <summary>Creates, observes, and sends Event Hubs message contexts through a supervised producer.</summary>
public interface EventHubSendTransportContext :
    SendTransportContext,
    IProbeSite
{
    /// <summary>Gets the agents whose lifetime is owned by a producer using this context.</summary>
    /// <returns>The supervised producer agents.</returns>
    IEnumerable<IAgent> GetAgentHandles();

    /// <summary>Creates and configures an Event Hubs send context for a typed message.</summary>
    /// <typeparam name="T">The message type.</typeparam>
    /// <param name="value">The outbound message.</param>
    /// <param name="pipe">The Event Hubs-specific context pipe.</param>
    /// <param name="initializerPipe">The optional message initializer pipe.</param>
    /// <param name="cancellationToken">Cancels context creation and configuration.</param>
    /// <returns>A task whose result is the configured send context.</returns>
    Task<EventHubSendContext<T>> CreateContextAsync<T>(T value, IPipe<EventHubSendContext<T>> pipe,
        IPipe<SendContext<T>>? initializerPipe = null, CancellationToken cancellationToken = default)
        where T : class;

    /// <summary>Serializes and sends one message through an active producer context.</summary>
    /// <typeparam name="T">The message type.</typeparam>
    /// <param name="producerContext">The active producer context.</param>
    /// <param name="sendContext">The configured outbound message context.</param>
    /// <param name="cancellationToken">Cancels the send before provider submission.</param>
    /// <returns>A task that completes when the Azure SDK producer finishes the send.</returns>
    Task SendAsync<T>(ProducerContext producerContext, EventHubSendContext<T> sendContext, CancellationToken cancellationToken = default)
        where T : class;

    /// <summary>Serializes and sends messages in one or more size-constrained Event Hubs batches.</summary>
    /// <typeparam name="T">The message type.</typeparam>
    /// <param name="producerContext">The active producer context.</param>
    /// <param name="sendContexts">The configured outbound message contexts.</param>
    /// <param name="cancellationToken">Cancels batching before provider submission.</param>
    /// <returns>A task that completes when all generated batches have been sent.</returns>
    Task SendAsync<T>(ProducerContext producerContext, EventHubSendContext<T>[] sendContexts, CancellationToken cancellationToken = default)
        where T : class;

    /// <summary>Runs a producer operation through the supervised context and host retry policy.</summary>
    /// <param name="pipe">The operation to execute with an active producer context.</param>
    /// <param name="cancellationToken">Cancels context acquisition, retry, or send execution.</param>
    /// <returns>The host-retry task that acquires a producer and executes <paramref name="pipe"/>.</returns>
    Task SendAsync(IPipe<ProducerContext> pipe, CancellationToken cancellationToken);
}
