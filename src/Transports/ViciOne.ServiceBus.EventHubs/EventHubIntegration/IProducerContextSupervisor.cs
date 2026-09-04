using ViciOne.ServiceBus.Transports;

namespace ViciOne.ServiceBus.EventHubs;

/// <summary>
/// Defines the contract for producer context supervisor.
/// </summary>
public interface IProducerContextSupervisor :
    ITransportSupervisor<ProducerContext>
{
}
