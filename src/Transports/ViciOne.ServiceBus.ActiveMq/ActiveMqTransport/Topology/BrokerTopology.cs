namespace ViciOne.ServiceBus.ActiveMq.Topology;

/// <summary>Provides the ActiveMQ topics, queues, and consumer bindings required by an operation.</summary>
public interface BrokerTopology :
    IProbeSite
{
    /// <summary>Gets the required topics.</summary>
    Topic[] Topics { get; }
    /// <summary>Gets the required queues.</summary>
    Queue[] Queues { get; }
    /// <summary>Gets the required consumer bindings.</summary>
    Consumer[] Consumers { get; }
}
