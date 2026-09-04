using ViciOne.ServiceBus.Transports;

namespace ViciOne.ServiceBus.ActiveMq;

/// <summary>
/// Creates and caches a session on the connection
/// </summary>
public interface ISessionContextSupervisor :
    ITransportSupervisor<SessionContext>
{
}
