// ViciOne modification: WP-F2-SERVICEBUS-IDENTITY, 2026-08-07.
namespace ViciOne.ServiceBus.SqlTransport
{
    using Transports;


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
}
