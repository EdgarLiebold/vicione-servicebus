using ViciOne.ServiceBus.Agents;
using ViciOne.ServiceBus.Transports;

namespace ViciOne.ServiceBus.AzureServiceBus;

/// <summary>
/// Provides a client context supervisor implementation.
/// </summary>
public class ClientContextSupervisor :
    TransportPipeContextSupervisor<ClientContext>,
    IClientContextSupervisor
{
    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="contextFactory">The context factory value.</param>
    public ClientContextSupervisor(IPipeContextFactory<ClientContext> contextFactory)
        : base(contextFactory)
    {
    }
}
