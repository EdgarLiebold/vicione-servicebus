namespace ViciOne.ServiceBus.AzureServiceBus.Topology;

/// <summary>Describes the Azure Service Bus entities and relationships to declare.</summary>
public interface BrokerTopology :
    IProbeSite
{
    /// <summary>Gets the topic declarations.</summary>
    Topic[] Topics { get; }
    /// <summary>Gets the queue declarations.</summary>
    Queue[] Queues { get; }
    /// <summary>Gets the consumer subscription declarations.</summary>
    Subscription[] Subscriptions { get; }
    /// <summary>Gets the topic-to-queue forwarding relationships.</summary>
    QueueSubscription[] QueueSubscriptions { get; }
    /// <summary>Gets the topic-to-topic forwarding relationships.</summary>
    TopicSubscription[] TopicSubscriptions { get; }
}
