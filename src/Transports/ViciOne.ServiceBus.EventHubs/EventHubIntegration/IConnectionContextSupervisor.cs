using ViciOne.ServiceBus.Transports;

namespace ViciOne.ServiceBus.EventHubs;

/// <summary>Supervises shared Event Hubs connection contexts and their dependent agents.</summary>
public interface IConnectionContextSupervisor :
    ITransportSupervisor<ConnectionContext>
{
}
