namespace ViciOne.ServiceBus.SqlTransport.Topology;

/// <summary>Defines the operations required by topic.</summary>
public interface Topic
{
    /// <summary>Gets the topic name.</summary>
    string TopicName { get; }
}
