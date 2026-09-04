using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Azure.Messaging.EventHubs;
using Azure.Messaging.EventHubs.Producer;

namespace ViciOne.ServiceBus.EventHubIntegration;

public interface ProducerContext :
    PipeContext,
    IAsyncDisposable
{
    Task ProduceAsync(EventDataBatch eventDataBatch, CancellationToken cancellationToken);
    Task ProduceAsync(IEnumerable<EventData> eventData, SendEventOptions options, CancellationToken cancellationToken);
    ValueTask<EventDataBatch> CreateBatchAsync(CreateBatchOptions options, CancellationToken cancellationToken);
}
