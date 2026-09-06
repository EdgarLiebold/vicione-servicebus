using ViciOne.ServiceBus.Topology;

namespace ViciOne.ServiceBus.ActiveMq.Topology;

/// <summary>Identifies a consumer binding created by a broker-topology builder.</summary>
public interface ConsumerHandle :
    EntityHandle
{
    /// <summary>Gets the represented consumer binding.</summary>
    Consumer Consumer { get; }
}
