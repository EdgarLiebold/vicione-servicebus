using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace ViciOne.ServiceBus.EventHubs;

/// <summary>Produces typed messages to a configured Event Hub.</summary>
public interface IEventHubProducer :
    ISendObserverConnector
{
    /// <summary>Produces one message to the configured Event Hub.</summary>
    /// <typeparam name="T">The message type.</typeparam>
    /// <param name="message">The message value.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The transport task for the serialized message.</returns>
    Task ProduceAsync<T>(T message, CancellationToken cancellationToken = default)
        where T : class;

    /// <summary>Produces a batch of messages to the configured Event Hub.</summary>
    /// <typeparam name="T">The message type.</typeparam>
    /// <param name="messages">The message values.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that completes when the provider finishes sending the batch.</returns>
    Task ProduceAsync<T>(IEnumerable<T> messages, CancellationToken cancellationToken = default)
        where T : class;

    /// <summary>Produces one message after applying Event Hubs send-context configuration.</summary>
    /// <typeparam name="T">The message type.</typeparam>
    /// <param name="message">The message value.</param>
    /// <param name="pipe">The pipe that configures the Event Hubs send context.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The transport task for the configured and serialized message.</returns>
    Task ProduceAsync<T>(T message, IPipe<EventHubSendContext<T>> pipe, CancellationToken cancellationToken = default)
        where T : class;

    /// <summary>Produces a batch after applying Event Hubs send-context configuration to each message.</summary>
    /// <typeparam name="T">The message type.</typeparam>
    /// <param name="messages">The message values.</param>
    /// <param name="pipe">The pipe applied to each Event Hubs send context.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The transport task for the configured provider-sized batches.</returns>
    Task ProduceAsync<T>(IEnumerable<T> messages, IPipe<EventHubSendContext<T>> pipe, CancellationToken cancellationToken = default)
        where T : class;

    /// <summary>Initializes and produces one message to the configured Event Hub.</summary>
    /// <typeparam name="T">The message type to initialize.</typeparam>
    /// <param name="values">The values used to initialize the message.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that completes after initialization and provider submission.</returns>
    Task ProduceAsync<T>(object values, CancellationToken cancellationToken = default)
        where T : class;

    /// <summary>Initializes and produces a batch of messages to the configured Event Hub.</summary>
    /// <typeparam name="T">The message type to initialize.</typeparam>
    /// <param name="values">The values used to initialize the messages.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that completes after initialization and batch submission.</returns>
    Task ProduceAsync<T>(IEnumerable<object> values, CancellationToken cancellationToken = default)
        where T : class;

    /// <summary>Initializes and produces one message after applying Event Hubs send-context configuration.</summary>
    /// <typeparam name="T">The message type to initialize.</typeparam>
    /// <param name="values">The values used to initialize the message.</param>
    /// <param name="pipe">The pipe that configures the Event Hubs send context.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that completes after initialization, configuration, and provider submission.</returns>
    Task ProduceAsync<T>(object values, IPipe<EventHubSendContext<T>> pipe, CancellationToken cancellationToken = default)
        where T : class;

    /// <summary>Initializes and produces a batch after applying Event Hubs send-context configuration to each message.</summary>
    /// <typeparam name="T">The message type to initialize.</typeparam>
    /// <param name="values">The values used to initialize the messages.</param>
    /// <param name="pipe">The pipe applied to each Event Hubs send context.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that completes after initialization, configuration, and batch submission.</returns>
    Task ProduceAsync<T>(IEnumerable<object> values, IPipe<EventHubSendContext<T>> pipe, CancellationToken cancellationToken = default)
        where T : class;
}
