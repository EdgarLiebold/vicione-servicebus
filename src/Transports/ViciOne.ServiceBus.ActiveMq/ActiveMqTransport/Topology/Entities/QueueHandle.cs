using ViciOne.ServiceBus.Topology;

namespace ViciOne.ServiceBus.ActiveMq.Topology;

/// <summary>Identifies a queue declaration created by a broker-topology builder.</summary>
public interface QueueHandle :
    EntityHandle
{
    /// <summary>Gets the represented queue declaration.</summary>
    Queue Queue { get; }
}
