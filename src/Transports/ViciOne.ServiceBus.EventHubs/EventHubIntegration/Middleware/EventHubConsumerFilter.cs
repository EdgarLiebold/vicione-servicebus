using System.Threading.Tasks;
using ViciOne.ServiceBus.Transports;

namespace ViciOne.ServiceBus.EventHubs.Middleware;

/// <summary>
/// Provides an event hub consumer filter implementation.
/// </summary>
public class EventHubConsumerFilter :
    IFilter<ProcessorContext>
{
    readonly ReceiveEndpointContext _context;

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="context">The operation context.</param>
    public EventHubConsumerFilter(ReceiveEndpointContext context)
    {
        _context = context;
    }

    /// <summary>
    /// Sends a message to the configured destination.
    /// </summary>
    /// <param name="context">The operation context.</param>
    /// <param name="next">The next value.</param>
    /// <returns>The result of the operation.</returns>
    public async Task SendAsync(ProcessorContext context, IPipe<ProcessorContext> next)
    {
        var receiveSettings = _context.GetPayload<ReceiveSettings>();

        var receiver = new EventHubDataReceiver(receiveSettings, _context, context);

        await receiver.Ready.ConfigureAwait(false);

        _context.AddConsumeAgent(receiver);

        await _context.TransportObservers.NotifyReadyAsync(_context.InputAddress).ConfigureAwait(false);

        try
        {
            await receiver.Completed.ConfigureAwait(false);
        }
        finally
        {
            DeliveryMetrics metrics = receiver;

            await _context.TransportObservers.NotifyCompletedAsync(_context.InputAddress, metrics).ConfigureAwait(false);

            _context.LogConsumerCompleted(metrics.DeliveryCount, metrics.ConcurrentDeliveryCount);
        }

        await next.SendAsync(context).ConfigureAwait(false);
    }

    /// <summary>
    /// Performs the probe operation.
    /// </summary>
    /// <param name="context">The operation context.</param>
    public void Probe(ProbeContext context)
    {
    }
}
