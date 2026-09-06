using ViciOne.ServiceBus.Topology;

namespace ViciOne.ServiceBus.AmazonSqs.Topology;

/// <summary>Identifies an Amazon SNS topic within a broker-topology builder.</summary>
public interface TopicHandle :
    EntityHandle
{
    /// <summary>Gets the topic declaration represented by the handle.</summary>
    Topic Topic { get; }
}
