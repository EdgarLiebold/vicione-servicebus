using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Azure.Messaging.EventHubs;
using Azure.Messaging.EventHubs.Producer;
using ViciOne.ServiceBus.Middleware;

namespace ViciOne.ServiceBus.EventHubs;

/// <summary>
/// Provides a shared producer context implementation.
/// </summary>
public class SharedProducerContext :
    ProxyPipeContext,
    ProducerContext
{
    readonly ProducerContext _context;

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="context">The operation context.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    public SharedProducerContext(ProducerContext context, CancellationToken cancellationToken)
        : base(context)
    {
        _context = context;
        CancellationToken = cancellationToken;
    }

    /// <summary>
    /// Gets the cancellation token value.
    /// </summary>
    public override CancellationToken CancellationToken { get; }

    /// <summary>
    /// Performs the produce operation.
    /// </summary>
    /// <param name="eventDataBatch">The event data batch value.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The result of the operation.</returns>
    public Task ProduceAsync(EventDataBatch eventDataBatch, CancellationToken cancellationToken)
    {
        return _context.ProduceAsync(eventDataBatch, cancellationToken);
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
        return _context.ProduceAsync(eventData, options, cancellationToken);
    }

    /// <summary>
    /// Creates batch.
    /// </summary>
    /// <param name="options">The options value.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The result of the operation.</returns>
    public ValueTask<EventDataBatch> CreateBatchAsync(CreateBatchOptions options, CancellationToken cancellationToken)
    {
        return _context.CreateBatchAsync(options, cancellationToken);
    }

    /// <summary>
    /// Releases the resources owned by this instance.
    /// </summary>
    /// <returns>The result of the operation.</returns>
    public ValueTask DisposeAsync()
    {
        return _context.DisposeAsync();
    }
}
