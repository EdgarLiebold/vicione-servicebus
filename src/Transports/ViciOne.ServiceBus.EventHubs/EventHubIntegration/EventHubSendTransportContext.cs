using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using ViciOne.ServiceBus.Transports;

namespace ViciOne.ServiceBus.EventHubs;

/// <summary>
/// Defines the contract for event hub send transport context.
/// </summary>
public interface EventHubSendTransportContext :
    SendTransportContext,
    IProbeSite
{
    /// <summary>
    /// Gets agent handles.
    /// </summary>
    /// <returns>The result of the operation.</returns>
    IEnumerable<IAgent> GetAgentHandles();

    /// <summary>
    /// Creates context.
    /// </summary>
    /// <typeparam name="T">The t type.</typeparam>
    /// <param name="value">The value.</param>
    /// <param name="pipe">The pipe value.</param>
    /// <param name="initializerPipe">The initializer pipe value.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The result of the operation.</returns>
    Task<EventHubSendContext<T>> CreateContextAsync<T>(T value, IPipe<EventHubSendContext<T>> pipe,
        IPipe<SendContext<T>>? initializerPipe = null, CancellationToken cancellationToken = default)
        where T : class;

    /// <summary>
    /// Sends a message to the configured destination.
    /// </summary>
    /// <typeparam name="T">The t type.</typeparam>
    /// <param name="producerContext">The producer context value.</param>
    /// <param name="sendContext">The send context value.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The result of the operation.</returns>
    Task SendAsync<T>(ProducerContext producerContext, EventHubSendContext<T> sendContext, CancellationToken cancellationToken = default)
        where T : class;

    /// <summary>
    /// Sends a message to the configured destination.
    /// </summary>
    /// <typeparam name="T">The t type.</typeparam>
    /// <param name="producerContext">The producer context value.</param>
    /// <param name="sendContexts">The send contexts value.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The result of the operation.</returns>
    Task SendAsync<T>(ProducerContext producerContext, EventHubSendContext<T>[] sendContexts, CancellationToken cancellationToken = default)
        where T : class;

    /// <summary>
    /// Sends a message to the configured destination.
    /// </summary>
    /// <param name="pipe">The pipe value.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The result of the operation.</returns>
    Task SendAsync(IPipe<ProducerContext> pipe, CancellationToken cancellationToken);
}
