using ViciOne.ServiceBus.Transports;

namespace ViciOne.ServiceBus.EventHubs;

/// <summary>Supervises the producer context for a single Event Hub.</summary>
public interface IProducerContextSupervisor :
    ITransportSupervisor<ProducerContext>
{
}
