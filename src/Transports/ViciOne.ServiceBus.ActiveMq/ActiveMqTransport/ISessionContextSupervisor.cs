using ViciOne.ServiceBus.Transports;

namespace ViciOne.ServiceBus.ActiveMq;

/// <summary>Supervises reusable Apache NMS session contexts for a connection.</summary>
public interface ISessionContextSupervisor :
    ITransportSupervisor<SessionContext>
{
}
