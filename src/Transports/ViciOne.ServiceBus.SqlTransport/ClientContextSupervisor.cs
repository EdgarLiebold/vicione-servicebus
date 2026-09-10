using ViciOne.ServiceBus.Transports;

namespace ViciOne.ServiceBus.SqlTransport;

/// <summary>Supervises the lifecycle of client context.</summary>
public class ClientContextSupervisor :
    TransportPipeContextSupervisor<ClientContext>,
    IClientContextSupervisor
{
    /// <summary>Initializes a new instance.</summary>
    /// <param name="connectionContextSupervisor">The connection context supervisor.</param>
    public ClientContextSupervisor(IConnectionContextSupervisor connectionContextSupervisor)
        : base(new ScopeClientContextFactory(connectionContextSupervisor))
    {
        connectionContextSupervisor.AddConsumeAgent(this);
    }

    /// <summary>Initializes a new instance.</summary>
    /// <param name="clientContextSupervisor">The client context supervisor.</param>
    public ClientContextSupervisor(IClientContextSupervisor clientContextSupervisor)
        : base(new SharedClientContextFactory(clientContextSupervisor))
    {
        clientContextSupervisor.AddSendAgent(this);
    }
}
