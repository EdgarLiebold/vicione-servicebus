using ViciOne.ServiceBus.Transports;

namespace ViciOne.ServiceBus.ActiveMq;

/// <summary>
/// Provides a session context supervisor implementation.
/// </summary>
public class SessionContextSupervisor :
    TransportPipeContextSupervisor<SessionContext>,
    ISessionContextSupervisor
{
    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="connectionContextSupervisor">The connection context supervisor value.</param>
    public SessionContextSupervisor(IConnectionContextSupervisor connectionContextSupervisor)
        : base(new SessionContextFactory(connectionContextSupervisor))
    {
        connectionContextSupervisor.AddConsumeAgent(this);
    }

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="sessionContextSupervisor">The session context supervisor value.</param>
    public SessionContextSupervisor(ISessionContextSupervisor sessionContextSupervisor)
        : base(new ScopeSessionContextFactory(sessionContextSupervisor))
    {
        sessionContextSupervisor.AddSendAgent(this);
    }
}
