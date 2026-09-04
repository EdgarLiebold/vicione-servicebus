using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Azure.Messaging.EventHubs;
using Azure.Messaging.EventHubs.Producer;
using ViciOne.ServiceBus.Middleware;

namespace ViciOne.ServiceBus.EventHubs;

/// <summary>
/// Provides an event hub producer context implementation.
/// </summary>
public class EventHubProducerContext :
    BasePipeContext,
    ProducerContext
{
    readonly EventHubProducerClient _producerClient;

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="producerClient">The producer client value.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    public EventHubProducerContext(EventHubProducerClient producerClient, CancellationToken cancellationToken)
        : base(cancellationToken)
    {
        _producerClient = producerClient;
    }

    /// <summary>
    /// Performs the produce operation.
    /// </summary>
    /// <param name="eventData">The event data value.</param>
    /// <param name="options">The options value.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The result of the operation.</returns>
    public Task ProduceAsync(IEnumerable<EventData> eventData, SendEventOptions options, CancellationToken cancellationToken)
    {
        return _producerClient.SendAsync(eventData, options, cancellationToken);
    }

    /// <summary>
    /// Performs the produce operation.
    /// </summary>
    /// <param name="eventDataBatch">The event data batch value.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The result of the operation.</returns>
    public Task ProduceAsync(EventDataBatch eventDataBatch, CancellationToken cancellationToken)
    {
        return _producerClient.SendAsync(eventDataBatch, cancellationToken);
    }

    /// <summary>
    /// Creates batch.
    /// </summary>
    /// <param name="options">The options value.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The result of the operation.</returns>
    public ValueTask<EventDataBatch> CreateBatchAsync(CreateBatchOptions options, CancellationToken cancellationToken)
    {
        return _producerClient.CreateBatchAsync(options, cancellationToken);
    }

    /// <summary>
    /// Releases the resources owned by this instance.
    /// </summary>
    /// <returns>The result of the operation.</returns>
    public ValueTask DisposeAsync()
    {
        return _producerClient.DisposeAsync();
    }
}
