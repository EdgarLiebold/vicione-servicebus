namespace ViciOne.ServiceBus.AmazonSqs.Topology;

/// <summary>Represents Amazon SNS topics, Amazon SQS queues, their subscriptions, and probe diagnostics.</summary>
public interface BrokerTopology :
    IProbeSite
{
    /// <summary>Gets the Amazon SNS topics.</summary>
    Topic[] Topics { get; }
    /// <summary>Gets the Amazon SQS queues.</summary>
    Queue[] Queues { get; }
    /// <summary>Gets the topic-to-queue subscriptions.</summary>
    QueueSubscription[] QueueSubscriptions { get; }
}
