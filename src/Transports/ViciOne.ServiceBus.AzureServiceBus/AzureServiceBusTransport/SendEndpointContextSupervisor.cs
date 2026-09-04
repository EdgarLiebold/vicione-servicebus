using ViciOne.ServiceBus.Agents;
using ViciOne.ServiceBus.Transports;

namespace ViciOne.ServiceBus.AzureServiceBus;

/// <summary>
/// Provides a send endpoint context supervisor implementation.
/// </summary>
public class SendEndpointContextSupervisor :
    TransportPipeContextSupervisor<SendEndpointContext>,
    ISendEndpointContextSupervisor
{
    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="contextFactory">The context factory value.</param>
    public SendEndpointContextSupervisor(IPipeContextFactory<SendEndpointContext> contextFactory)
        : base(contextFactory)
    {
    }
}
