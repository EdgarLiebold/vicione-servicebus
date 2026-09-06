using ViciOne.ServiceBus.Transports;

namespace ViciOne.ServiceBus.EventHubs;

/// <summary>Supervises the Event Hubs processor context used by a receive endpoint.</summary>
public interface IProcessorContextSupervisor :
    ITransportSupervisor<ProcessorContext>
{
}
