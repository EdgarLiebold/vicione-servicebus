// ViciOne modification: WP-F2-SERVICEBUS-IDENTITY, 2026-08-07.
namespace ViciOne.ServiceBus.ActiveMqTransport
{
    using Transports;


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
}
