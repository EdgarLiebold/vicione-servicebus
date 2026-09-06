namespace ViciOne.ServiceBus.AzureServiceBus.Middleware;

/// <summary>Creates a session-aware receiver for an endpoint client context.</summary>
public class MessageSessionReceiverFilter :
    MessageReceiverFilter
{
    /// <summary>Initializes the filter for a session-enabled receive endpoint.</summary>
    /// <param name="context">The endpoint whose session receiver is coordinated.</param>
    public MessageSessionReceiverFilter(ServiceBusReceiveEndpointContext context)
        : base(context)
    {
    }

    /// <summary>Writes diagnostic information to the probe context.</summary>
    /// <param name="context">The probe section to populate.</param>
    public override void Probe(ProbeContext context)
    {
        var scope = context.CreateFilterScope("messageSessionReceiver");
        scope.Add("type", "brokeredMessage");

        Context.ReceivePipe.Probe(scope);
    }

    /// <summary>Creates the session receiver agent for the client context.</summary>
    /// <param name="context">The initialized session processor client.</param>
    /// <returns>The session receiver agent.</returns>
    protected override IReceiver CreateMessageReceiver(ClientContext context)
    {
        return new SessionReceiver(context, Context);
    }
}
