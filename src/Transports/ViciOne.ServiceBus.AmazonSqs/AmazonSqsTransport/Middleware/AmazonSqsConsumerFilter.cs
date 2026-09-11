using System.Threading.Tasks;
using ViciOne.ServiceBus.Transports;

namespace ViciOne.ServiceBus.AmazonSqs.Middleware;
/// <summary>Runs an Amazon SQS polling receiver for the lifetime of a client-context pipeline.</summary>
public class AmazonSqsConsumerFilter :
    IFilter<ClientContext>
{
    readonly SqsReceiveEndpointContext _context;

    /// <summary>Initializes an Amazon SQS consumer filter.</summary>
    /// <param name="context">The receive endpoint that owns the receiver and its metrics.</param>
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
            IDeliveryMetrics metrics = receiver;

            await _context.TransportObservers.NotifyCompletedAsync(_context.InputAddress, metrics).ConfigureAwait(false);

            _context.LogConsumerCompleted(metrics.DeliveryCount, metrics.MaxConcurrentDeliveryCount);
        }
    }
}
