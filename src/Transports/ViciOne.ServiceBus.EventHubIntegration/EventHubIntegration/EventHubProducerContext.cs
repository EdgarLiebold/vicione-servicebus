using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Azure.Messaging.EventHubs;
using Azure.Messaging.EventHubs.Producer;
using ViciOne.ServiceBus.Middleware;

namespace ViciOne.ServiceBus.EventHubIntegration;

public class EventHubProducerContext :
    BasePipeContext,
    ProducerContext
{
    readonly EventHubProducerClient _producerClient;

    public EventHubProducerContext(EventHubProducerClient producerClient, CancellationToken cancellationToken)
        : base(cancellationToken)
    {
        _producerClient = producerClient;
    }

    public Task ProduceAsync(IEnumerable<EventData> eventData, SendEventOptions options, CancellationToken cancellationToken)
    {
        return _producerClient.SendAsync(eventData, options, cancellationToken);
    }

    public Task ProduceAsync(EventDataBatch eventDataBatch, CancellationToken cancellationToken)
    {
        return _producerClient.SendAsync(eventDataBatch, cancellationToken);
    }

    public ValueTask<EventDataBatch> CreateBatchAsync(CreateBatchOptions options, CancellationToken cancellationToken)
    {
        return _producerClient.CreateBatchAsync(options, cancellationToken);
    }

    public ValueTask DisposeAsync()
    {
        return _producerClient.DisposeAsync();
    }
}
