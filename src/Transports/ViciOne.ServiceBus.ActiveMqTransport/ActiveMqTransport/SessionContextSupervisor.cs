using ViciOne.ServiceBus.Transports;

namespace ViciOne.ServiceBus.ActiveMqTransport;

public class SessionContextSupervisor :
    TransportPipeContextSupervisor<SessionContext>,
    ISessionContextSupervisor
{
    public SessionContextSupervisor(IConnectionContextSupervisor connectionContextSupervisor)
        : base(new SessionContextFactory(connectionContextSupervisor))
    {
        connectionContextSupervisor.AddConsumeAgent(this);
    }

    public SessionContextSupervisor(ISessionContextSupervisor sessionContextSupervisor)
        : base(new ScopeSessionContextFactory(sessionContextSupervisor))
    {
        sessionContextSupervisor.AddSendAgent(this);
    }
}
