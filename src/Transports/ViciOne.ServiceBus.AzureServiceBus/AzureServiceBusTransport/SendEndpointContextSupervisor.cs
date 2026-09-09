using ViciOne.ServiceBus.Advanced.Middleware;
using ViciOne.ServiceBus.Transports;

namespace ViciOne.ServiceBus.AzureServiceBus;

/// <summary>Supervises the lifecycle and recycling of an Azure Service Bus sender context.</summary>
public class SendEndpointContextSupervisor :
    TransportPipeContextSupervisor<SendEndpointContext>,
    ISendEndpointContextSupervisor
{
    /// <summary>Creates a supervisor around a sender-context factory.</summary>
    /// <param name="contextFactory">The factory used to create and recreate sender contexts.</param>
    public SendEndpointContextSupervisor(IPipeContextFactory<SendEndpointContext> contextFactory)
        : base(contextFactory)
    {
    }
}
