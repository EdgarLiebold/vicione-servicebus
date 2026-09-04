using ViciOne.ServiceBus.Transports;

namespace ViciOne.ServiceBus.ActiveMqTransport;

/// <summary>
/// Creates and caches a session on the connection
/// </summary>
public interface ISessionContextSupervisor :
    ITransportSupervisor<SessionContext>
{
}
