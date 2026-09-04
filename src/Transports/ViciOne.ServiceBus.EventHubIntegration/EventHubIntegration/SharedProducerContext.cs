using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Azure.Messaging.EventHubs;
using Azure.Messaging.EventHubs.Producer;
using ViciOne.ServiceBus.Middleware;

namespace ViciOne.ServiceBus.EventHubIntegration;

public class SharedProducerContext :
    ProxyPipeContext,
    ProducerContext
{
    readonly ProducerContext _context;

    public SharedProducerContext(ProducerContext context, CancellationToken cancellationToken)
        : base(context)
    {
        _context = context;
        CancellationToken = cancellationToken;
    }

    public override CancellationToken CancellationToken { get; }

    public Task ProduceAsync(EventDataBatch eventDataBatch, CancellationToken cancellationToken)
    {
        return _context.ProduceAsync(eventDataBatch, cancellationToken);
    }

    public Task ProduceAsync(IEnumerable<EventData> eventData, SendEventOptions options, CancellationToken cancellationToken)
    {
        return _context.ProduceAsync(eventData, options, cancellationToken);
    }

    public ValueTask<EventDataBatch> CreateBatchAsync(CreateBatchOptions options, CancellationToken cancellationToken)
    {
        return _context.CreateBatchAsync(options, cancellationToken);
    }

    public ValueTask DisposeAsync()
    {
        return _context.DisposeAsync();
    }
}
