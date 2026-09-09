using ViciOne.ServiceBus.Advanced.Middleware;
using ViciOne.ServiceBus.Transports;

namespace ViciOne.ServiceBus.AzureServiceBus;

/// <summary>Supervises creation, sharing, and disposal of an Azure Service Bus client context.</summary>
public class ClientContextSupervisor :
    TransportPipeContextSupervisor<ClientContext>,
    IClientContextSupervisor
{
    /// <summary>Initializes the supervisor with its client-context factory.</summary>
    /// <param name="contextFactory">The factory used when the supervised context must be created or recycled.</param>
    public ClientContextSupervisor(IPipeContextFactory<ClientContext> contextFactory)
        : base(contextFactory)
    {
    }
}
