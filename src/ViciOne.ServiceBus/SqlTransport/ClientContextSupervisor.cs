using ViciOne.ServiceBus.Transports;

namespace ViciOne.ServiceBus.SqlTransport;

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
    /// <param name="connectionContextSupervisor">The connection context supervisor value.</param>
    public ClientContextSupervisor(IConnectionContextSupervisor connectionContextSupervisor)
        : base(new ScopeClientContextFactory(connectionContextSupervisor))
    {
        connectionContextSupervisor.AddConsumeAgent(this);
    }

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="clientContextSupervisor">The client context supervisor value.</param>
    public ClientContextSupervisor(IClientContextSupervisor clientContextSupervisor)
        : base(new SharedClientContextFactory(clientContextSupervisor))
    {
        clientContextSupervisor.AddSendAgent(this);
    }
}
