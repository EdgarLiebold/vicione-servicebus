using ViciOne.ServiceBus.Transports;

namespace ViciOne.ServiceBus.AmazonSqs;

/// <summary>Supervises shared or scoped Amazon client-context lifetimes.</summary>
public class ClientContextSupervisor :
    TransportPipeContextSupervisor<ClientContext>,
    IClientContextSupervisor
{
    /// <summary>Initializes a client supervisor backed by a connection supervisor.</summary>
    /// <param name="connectionContextSupervisor">The connection supervisor and parent consume agent.</param>
    public ClientContextSupervisor(IConnectionContextSupervisor connectionContextSupervisor)
        : base(new ClientContextFactory(connectionContextSupervisor))
    {
        connectionContextSupervisor.AddConsumeAgent(this);
    }

    /// <summary>Initializes a scoped client supervisor backed by another client supervisor.</summary>
    /// <param name="clientContextSupervisor">The parent client supervisor and send agent.</param>
    public ClientContextSupervisor(IClientContextSupervisor clientContextSupervisor)
        : base(new ScopeClientContextFactory(clientContextSupervisor))
    {
        clientContextSupervisor.AddSendAgent(this);
    }
}
