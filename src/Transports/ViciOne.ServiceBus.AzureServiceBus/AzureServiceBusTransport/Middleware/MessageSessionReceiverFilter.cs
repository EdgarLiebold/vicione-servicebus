namespace ViciOne.ServiceBus.AzureServiceBus.Middleware;

/// <summary>
/// Creates a message session receiver
/// </summary>
public class MessageSessionReceiverFilter :
    MessageReceiverFilter
{
    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="context">The operation context.</param>
    public MessageSessionReceiverFilter(ServiceBusReceiveEndpointContext context)
        : base(context)
    {
    }

    /// <summary>
    /// Performs the probe operation.
    /// </summary>
    /// <param name="context">The operation context.</param>
    public override void Probe(ProbeContext context)
    {
        var scope = context.CreateFilterScope("messageSessionReceiver");
        scope.Add("type", "brokeredMessage");

        Context.ReceivePipe.Probe(scope);
    }

    /// <summary>
    /// Creates message receiver.
    /// </summary>
    /// <param name="context">The operation context.</param>
    /// <returns>The result of the operation.</returns>
    protected override IReceiver CreateMessageReceiver(ClientContext context)
    {
        return new SessionReceiver(context, Context);
    }
}
