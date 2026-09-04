using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Azure.Messaging.EventHubs;
using Azure.Messaging.EventHubs.Producer;

namespace ViciOne.ServiceBus.EventHubs;

/// <summary>
/// Defines the contract for producer context.
/// </summary>
public interface ProducerContext :
    PipeContext,
    IAsyncDisposable
{
    /// <summary>
    /// Performs the produce operation.
    /// </summary>
    /// <param name="eventDataBatch">The event data batch value.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The result of the operation.</returns>
    Task ProduceAsync(EventDataBatch eventDataBatch, CancellationToken cancellationToken);
    /// <summary>
    /// Performs the produce operation.
    /// </summary>
    /// <param name="eventData">The event data value.</param>
    /// <param name="options">The options value.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The result of the operation.</returns>
    Task ProduceAsync(IEnumerable<EventData> eventData, SendEventOptions options, CancellationToken cancellationToken);
    /// <summary>
    /// Creates batch.
    /// </summary>
    /// <param name="options">The options value.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The result of the operation.</returns>
    ValueTask<EventDataBatch> CreateBatchAsync(CreateBatchOptions options, CancellationToken cancellationToken);
}
