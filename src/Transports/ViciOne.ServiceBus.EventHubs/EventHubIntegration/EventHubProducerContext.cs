using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Azure.Messaging.EventHubs;
using Azure.Messaging.EventHubs.Producer;
using ViciOne.ServiceBus.Middleware;

namespace ViciOne.ServiceBus.EventHubs;

/// <summary>Adapts an Azure SDK producer client to the transport producer-context contract.</summary>
public class EventHubProducerContext :
    BasePipeContext,
    ProducerContext
{
    readonly EventHubProducerClient _producerClient;

    /// <summary>Creates a producer context that owns the supplied client.</summary>
    /// <param name="producerClient">The Azure SDK producer client.</param>
    /// <param name="cancellationToken">Stops operations using this context.</param>
    public EventHubProducerContext(EventHubProducerClient producerClient, CancellationToken cancellationToken)
        : base(cancellationToken)
    {
        _producerClient = producerClient;
    }

    /// <summary>Sends a collection of events with common partition-routing options.</summary>
    /// <param name="eventData">The events to send.</param>
    /// <param name="options">The partition routing options.</param>
    /// <param name="cancellationToken">Cancels the send operation.</param>
    /// <returns>The task returned by the Azure SDK producer client.</returns>
    public Task ProduceAsync(IEnumerable<EventData> eventData, SendEventOptions options, CancellationToken cancellationToken)
    {
        return _producerClient.SendAsync(eventData, options, cancellationToken);
    }

    /// <summary>Sends a completed Azure SDK event batch.</summary>
    /// <param name="eventDataBatch">The batch to send.</param>
    /// <param name="cancellationToken">Cancels the send operation.</param>
    /// <returns>The task returned by the Azure SDK producer client.</returns>
    public Task ProduceAsync(EventDataBatch eventDataBatch, CancellationToken cancellationToken)
    {
        return _producerClient.SendAsync(eventDataBatch, cancellationToken);
    }

    /// <summary>Creates a size-aware event batch with the requested partition routing.</summary>
    /// <param name="options">The batch partition routing options.</param>
    /// <param name="cancellationToken">Cancels batch creation.</param>
    /// <returns>The value task returned by the Azure SDK producer client.</returns>
    public ValueTask<EventDataBatch> CreateBatchAsync(CreateBatchOptions options, CancellationToken cancellationToken)
    {
        return _producerClient.CreateBatchAsync(options, cancellationToken);
    }

    /// <summary>Disposes the owned Azure SDK producer client.</summary>
    /// <returns>The asynchronous disposal operation of the owned Azure SDK client.</returns>
    public ValueTask DisposeAsync()
    {
        return _producerClient.DisposeAsync();
    }
}
