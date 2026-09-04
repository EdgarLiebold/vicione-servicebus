using ViciOne.ServiceBus.Transports;

namespace ViciOne.ServiceBus.SqlTransport;

public class ClientContextSupervisor :
    TransportPipeContextSupervisor<ClientContext>,
    IClientContextSupervisor
{
    public ClientContextSupervisor(IConnectionContextSupervisor connectionContextSupervisor)
        : base(new ScopeClientContextFactory(connectionContextSupervisor))
    {
        connectionContextSupervisor.AddConsumeAgent(this);
    }

    public ClientContextSupervisor(IClientContextSupervisor clientContextSupervisor)
        : base(new SharedClientContextFactory(clientContextSupervisor))
    {
        clientContextSupervisor.AddSendAgent(this);
    }
}
