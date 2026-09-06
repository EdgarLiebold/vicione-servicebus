using ViciOne.ServiceBus.Topology;

namespace ViciOne.ServiceBus.ActiveMq.Topology;

/// <summary>Identifies a topic declaration created by a broker-topology builder.</summary>
public interface TopicHandle :
    EntityHandle
{
    /// <summary>Gets the represented topic declaration.</summary>
    Topic Topic { get; }
}
