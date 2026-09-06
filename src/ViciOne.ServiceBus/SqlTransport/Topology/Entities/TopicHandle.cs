using ViciOne.ServiceBus.Topology;

namespace ViciOne.ServiceBus.SqlTransport.Topology;

/// <summary>Controls the lifetime of topic.</summary>
public interface TopicHandle :
    EntityHandle
{
    /// <summary>Gets the topic.</summary>
    Topic Topic { get; }
}
