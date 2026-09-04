using ViciOne.ServiceBus.Transports;

namespace ViciOne.ServiceBus.EventHubs;

/// <summary>
/// Defines the contract for processor context supervisor.
/// </summary>
public interface IProcessorContextSupervisor :
    ITransportSupervisor<ProcessorContext>
{
}
