using ViciOne.ServiceBus.Transports;

namespace ViciOne.ServiceBus.AmazonSqs;

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
        : base(new ClientContextFactory(connectionContextSupervisor))
    {
        connectionContextSupervisor.AddConsumeAgent(this);
    }

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="clientContextSupervisor">The client context supervisor value.</param>
    public ClientContextSupervisor(IClientContextSupervisor clientContextSupervisor)
        : base(new ScopeClientContextFactory(clientContextSupervisor))
    {
        clientContextSupervisor.AddSendAgent(this);
    }
}
