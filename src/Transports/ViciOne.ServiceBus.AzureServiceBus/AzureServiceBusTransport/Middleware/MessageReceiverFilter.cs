using System.Threading.Tasks;
using ViciOne.ServiceBus.Transports;

namespace ViciOne.ServiceBus.AzureServiceBus.Middleware;

/// <summary>Starts a receiver for an endpoint client context and reports its readiness, completion, and delivery metrics.</summary>
public class MessageReceiverFilter :
    IFilter<ClientContext>
{
    readonly IReceiveTransportObserver _transportObserver;
    /// <summary>Provides the receive-endpoint context used by specialized receiver filters.</summary>
    protected readonly ServiceBusReceiveEndpointContext Context;

    /// <summary>Initializes the filter for a receive endpoint.</summary>
    /// <param name="context">The endpoint whose client and observer lifecycles are coordinated.</param>
    public MessageReceiverFilter(ServiceBusReceiveEndpointContext context)
    {
        _transportObserver = context.TransportObservers;
        Context = context;
    }

    /// <summary>Writes diagnostic information to the probe context.</summary>
    /// <param name="context">The probe section to populate.</param>
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
            IDeliveryMetrics metrics = receiver;

            await _transportObserver.NotifyCompletedAsync(Context.InputAddress, metrics).ConfigureAwait(false);

            Context.LogConsumerCompleted(metrics.DeliveryCount, metrics.MaxConcurrentDeliveryCount);
        }

        await next.SendAsync(context).ConfigureAwait(false);
    }

    /// <summary>Creates the receiver agent for the client context.</summary>
    /// <param name="context">The initialized processor client.</param>
    /// <returns>The receiver agent.</returns>
    protected virtual IReceiver CreateMessageReceiver(ClientContext context)
    {
        return new Receiver(context, Context);
    }
}
