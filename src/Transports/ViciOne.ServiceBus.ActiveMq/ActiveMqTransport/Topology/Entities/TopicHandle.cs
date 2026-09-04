using ViciOne.ServiceBus.Topology;

namespace ViciOne.ServiceBus.ActiveMq.Topology;

/// <summary>
/// Defines the contract for topic handle.
/// </summary>
public interface TopicHandle :
    EntityHandle
{
    /// <summary>
    /// Gets the topic value.
    /// </summary>
    Topic Topic { get; }
}
