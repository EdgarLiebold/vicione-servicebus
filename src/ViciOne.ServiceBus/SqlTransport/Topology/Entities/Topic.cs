namespace ViciOne.ServiceBus.SqlTransport.Topology;

/// <summary>
/// Defines the contract for topic.
/// </summary>
public interface Topic
{
    /// <summary>
    /// Gets the topic name value.
    /// </summary>
    string TopicName { get; }
}
