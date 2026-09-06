using ViciOne.ServiceBus.Transports;

namespace ViciOne.ServiceBus.EventHubs;

/// <summary>Supervises a producer context and registers it as a dependent send agent of the shared connection.</summary>
public class ProducerContextSupervisor :
    TransportPipeContextSupervisor<ProducerContext>,
    IProducerContextSupervisor
{
    /// <summary>Creates a supervisor for one Event Hub producer.</summary>
    /// <param name="contextSupervisor">The shared Event Hubs connection supervisor.</param>
    /// <param name="eventHubName">The Event Hub entity name.</param>
    public ProducerContextSupervisor(IConnectionContextSupervisor contextSupervisor, string eventHubName)
        : base(new ProducerContextFactory(contextSupervisor, eventHubName))
    {
        contextSupervisor.AddSendAgent(this);
    }
}
