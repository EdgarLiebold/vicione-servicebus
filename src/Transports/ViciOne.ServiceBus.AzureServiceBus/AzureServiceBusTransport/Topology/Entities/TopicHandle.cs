using ViciOne.ServiceBus.Topology;

namespace ViciOne.ServiceBus.AzureServiceBus.Topology;

/// <summary>Identifies a topic stored in a broker-topology builder.</summary>
public interface TopicHandle :
    EntityHandle
{
    /// <summary>Gets the topic declaration represented by the handle.</summary>
    Topic Topic { get; }
}
