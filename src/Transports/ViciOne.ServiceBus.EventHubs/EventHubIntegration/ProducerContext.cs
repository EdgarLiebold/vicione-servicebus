using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Azure.Messaging.EventHubs;
using Azure.Messaging.EventHubs.Producer;

namespace ViciOne.ServiceBus.EventHubs;

/// <summary>Wraps an Azure SDK producer client for one Event Hub.</summary>
public interface ProducerContext :
    PipeContext,
    IAsyncDisposable
{
    /// <summary>Sends a completed Azure SDK event batch.</summary>
    /// <param name="eventDataBatch">The batch to send.</param>
    /// <param name="cancellationToken">Cancels the send operation.</param>
    /// <returns>A task that completes when the producer client finishes sending the batch.</returns>
    Task ProduceAsync(EventDataBatch eventDataBatch, CancellationToken cancellationToken);
    /// <summary>Sends a collection of Azure SDK events with common partition-routing options.</summary>
    /// <param name="eventData">The events to send.</param>
    /// <param name="options">The partition routing options for the send operation.</param>
    /// <param name="cancellationToken">Cancels the send operation.</param>
    /// <returns>A task that completes when the producer client finishes sending the events.</returns>
    Task ProduceAsync(IEnumerable<EventData> eventData, SendEventOptions options, CancellationToken cancellationToken);
    /// <summary>Creates an Azure SDK event batch with the requested partition routing.</summary>
    /// <param name="options">The batch partition routing options.</param>
    /// <param name="cancellationToken">Cancels batch creation.</param>
    /// <returns>A task whose result is a size-aware event batch.</returns>
    ValueTask<EventDataBatch> CreateBatchAsync(CreateBatchOptions options, CancellationToken cancellationToken);
}
