using ViciOne.ServiceBus.Topology;

namespace ViciOne.ServiceBus.SqlTransport.Topology;

/// <summary>Identifies a topic within its owning SQL topology builder.</summary>
public interface TopicHandle :
    EntityHandle
{
    /// <summary>Gets the topic.</summary>
    Topic Topic { get; }
}
