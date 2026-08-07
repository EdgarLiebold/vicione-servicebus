// ViciOne modification: WP-F2-SERVICEBUS-IDENTITY, 2026-08-07.
namespace ViciOne.ServiceBus.EventHubIntegration
{
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;
    using Azure.Messaging.EventHubs;
    using Azure.Messaging.EventHubs.Producer;
    using ViciOne.ServiceBus.Middleware;


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

        public Task Produce(IEnumerable<EventData> eventData, SendEventOptions options, CancellationToken cancellationToken)
        {
            return _producerClient.SendAsync(eventData, options, cancellationToken);
        }

        public Task Produce(EventDataBatch eventDataBatch, CancellationToken cancellationToken)
        {
            return _producerClient.SendAsync(eventDataBatch, cancellationToken);
        }

        public ValueTask<EventDataBatch> CreateBatch(CreateBatchOptions options, CancellationToken cancellationToken)
        {
            return _producerClient.CreateBatchAsync(options, cancellationToken);
        }

        public ValueTask DisposeAsync()
        {
            return _producerClient.DisposeAsync();
        }
    }
}
