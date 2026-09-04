using System.Threading.Tasks;
using ViciOne.ServiceBus.Transports;

namespace ViciOne.ServiceBus.AmazonSqs.Middleware;
/// <summary>
/// A filter that uses the model context to create a basic consumer and connect it to the model
/// </summary>
public class AmazonSqsConsumerFilter :
    IFilter<ClientContext>
{
    readonly SqsReceiveEndpointContext _context;

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="context">The operation context.</param>
    public AmazonSqsConsumerFilter(SqsReceiveEndpointContext context)
    {
        _context = context;
    }

    void IProbeSite.Probe(ProbeContext context)
    {
    }

    async Task IFilter<ClientContext>.SendAsync(ClientContext context, IPipe<ClientContext> next)
    {
        var receiver = new AmazonSqsMessageReceiver(context, _context);

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
    }
}
