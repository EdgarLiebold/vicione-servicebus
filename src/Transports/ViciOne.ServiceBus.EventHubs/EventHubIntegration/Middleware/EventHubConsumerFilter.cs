using System.Threading.Tasks;
using ViciOne.ServiceBus.Transports;

namespace ViciOne.ServiceBus.EventHubs.Middleware;

/// <summary>Starts an Event Hubs data receiver, publishes transport lifecycle notifications, and records final delivery metrics.</summary>
public class EventHubConsumerFilter :
    IFilter<ProcessorContext>
{
    readonly ReceiveEndpointContext _context;

    /// <summary>Creates the filter for a receive endpoint.</summary>
    /// <param name="context">The receive-endpoint context that owns the receiver.</param>
    public EventHubConsumerFilter(ReceiveEndpointContext context)
    {
        _context = context;
    }

    /// <summary>Runs the data receiver to completion before continuing the processor pipeline.</summary>
    /// <param name="context">The active Event Hubs processor context.</param>
    /// <param name="next">The remaining processor pipeline.</param>
    /// <returns>A task that completes after receiver shutdown and downstream execution.</returns>
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
            IDeliveryMetrics metrics = receiver;

            await _context.TransportObservers.NotifyCompletedAsync(_context.InputAddress, metrics).ConfigureAwait(false);

            _context.LogConsumerCompleted(metrics.DeliveryCount, metrics.MaxConcurrentDeliveryCount);
        }

        await next.SendAsync(context).ConfigureAwait(false);
    }

    /// <summary>Writes diagnostic information to the probe context.</summary>
    /// <param name="context">The probe context; this filter does not add probe fields.</param>
    public void Probe(ProbeContext context)
    {
    }
}
