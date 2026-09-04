using ViciOne.ServiceBus.Transports;

namespace ViciOne.ServiceBus.EventHubs;

/// <summary>
/// Provides a producer context supervisor implementation.
/// </summary>
public class ProducerContextSupervisor :
    TransportPipeContextSupervisor<ProducerContext>,
    IProducerContextSupervisor
{
    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="contextSupervisor">The context supervisor value.</param>
    /// <param name="eventHubName">The event hub name value.</param>
    public ProducerContextSupervisor(IConnectionContextSupervisor contextSupervisor, string eventHubName)
        : base(new ProducerContextFactory(contextSupervisor, eventHubName))
    {
        contextSupervisor.AddSendAgent(this);
    }
}
