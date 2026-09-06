using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Azure.Messaging.EventHubs;
using Azure.Messaging.EventHubs.Producer;
using ViciOne.ServiceBus.Middleware;

namespace ViciOne.ServiceBus.EventHubs;

/// <summary>Projects a shared producer context onto a caller-specific cancellation token.</summary>
public class SharedProducerContext :
    ProxyPipeContext,
    ProducerContext
{
    readonly ProducerContext _context;

    /// <summary>Creates a proxy over a shared producer context.</summary>
    /// <param name="context">The shared producer context.</param>
    /// <param name="cancellationToken">The cancellation token exposed by this proxy.</param>
    public SharedProducerContext(ProducerContext context, CancellationToken cancellationToken)
        : base(context)
    {
        _context = context;
        CancellationToken = cancellationToken;
    }

    /// <summary>Gets the cancellation token.</summary>
    public override CancellationToken CancellationToken { get; }

    /// <summary>Sends a completed event batch through the shared producer.</summary>
    /// <param name="eventDataBatch">The batch to send.</param>
    /// <param name="cancellationToken">Cancels the send operation.</param>
    /// <returns>The task returned by the shared producer context.</returns>
    public Task ProduceAsync(EventDataBatch eventDataBatch, CancellationToken cancellationToken)
    {
        return _context.ProduceAsync(eventDataBatch, cancellationToken);
    }

    /// <summary>Sends events with common partition-routing options through the shared producer.</summary>
    /// <param name="eventData">The events to send.</param>
    /// <param name="options">The partition routing options.</param>
    /// <param name="cancellationToken">Cancels the send operation.</param>
    /// <returns>The task returned by the shared producer context.</returns>
    public Task ProduceAsync(IEnumerable<EventData> eventData, SendEventOptions options, CancellationToken cancellationToken)
    {
        return _context.ProduceAsync(eventData, options, cancellationToken);
    }

    /// <summary>Creates an event batch through the shared producer.</summary>
    /// <param name="options">The batch partition routing options.</param>
    /// <param name="cancellationToken">Cancels batch creation.</param>
    /// <returns>The value task returned by the shared producer context.</returns>
    public ValueTask<EventDataBatch> CreateBatchAsync(CreateBatchOptions options, CancellationToken cancellationToken)
    {
        return _context.CreateBatchAsync(options, cancellationToken);
    }

    /// <summary>Disposes the underlying shared producer context.</summary>
    /// <returns>The underlying producer context's asynchronous disposal operation.</returns>
    public ValueTask DisposeAsync()
    {
        return _context.DisposeAsync();
    }
}
