using System.Threading.Tasks;
using ViciOne.ServiceBus.Transports;

namespace ViciOne.ServiceBus.AzureServiceBusTransport.Middleware;

/// <summary>
/// Creates a message receiver and receives messages from the input queue of the endpoint
/// </summary>
public class MessageReceiverFilter :
    IFilter<ClientContext>
{
    readonly IReceiveTransportObserver _transportObserver;
    protected readonly ServiceBusReceiveEndpointContext Context;

    public MessageReceiverFilter(ServiceBusReceiveEndpointContext context)
    {
        _transportObserver = context.TransportObservers;
        Context = context;
    }

    public virtual void Probe(ProbeContext context)
    {
        var scope = context.CreateFilterScope("messageReceiver");
        scope.Add("type", "brokeredMessage");

        Context.ReceivePipe.Probe(scope);
    }

    async Task IFilter<ClientContext>.SendAsync(ClientContext context, IPipe<ClientContext> next)
    {
        if (context.IsClosedOrClosing)
            return;

        var receiver = CreateMessageReceiver(context);

        receiver.Start();

        await receiver.Ready.ConfigureAwait(false);

        Context.AddConsumeAgent(receiver);

        await _transportObserver.NotifyReadyAsync(Context.InputAddress).ConfigureAwait(false);

        try
        {
            await receiver.Completed.ConfigureAwait(false);
        }
        finally
        {
            DeliveryMetrics metrics = receiver;

            await _transportObserver.NotifyCompletedAsync(Context.InputAddress, metrics).ConfigureAwait(false);

            Context.LogConsumerCompleted(metrics.DeliveryCount, metrics.ConcurrentDeliveryCount);
        }

        await next.SendAsync(context).ConfigureAwait(false);
    }

    protected virtual IReceiver CreateMessageReceiver(ClientContext context)
    {
        return new Receiver(context, Context);
    }
}
