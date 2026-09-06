using ViciOne.ServiceBus.Transports;

namespace ViciOne.ServiceBus.ActiveMq;

/// <summary>Supervises Apache NMS session contexts for receive or send agents.</summary>
public class SessionContextSupervisor :
    TransportPipeContextSupervisor<SessionContext>,
    ISessionContextSupervisor
{
    /// <summary>Creates a receive-side session supervisor on a broker connection.</summary>
    /// <param name="connectionContextSupervisor">The parent connection supervisor.</param>
    public SessionContextSupervisor(IConnectionContextSupervisor connectionContextSupervisor)
        : base(new SessionContextFactory(connectionContextSupervisor))
    {
        connectionContextSupervisor.AddConsumeAgent(this);
    }

    /// <summary>Creates a send-side scoped session supervisor on another session supervisor.</summary>
    /// <param name="sessionContextSupervisor">The parent session supervisor.</param>
    public SessionContextSupervisor(ISessionContextSupervisor sessionContextSupervisor)
        : base(new ScopeSessionContextFactory(sessionContextSupervisor))
    {
        sessionContextSupervisor.AddSendAgent(this);
    }
}
