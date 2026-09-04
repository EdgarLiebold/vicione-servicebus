using System.Collections.Generic;
using ViciOne.ServiceBus.SqlTransport.Topology;

#nullable enable
namespace ViciOne.ServiceBus.SqlTransport.Configuration;

public class QueueSubscriptionConsumeTopologySpecification :
    SqlTopicSubscriptionConfigurator,
    ISqlConsumeTopologySpecification
{
    public QueueSubscriptionConsumeTopologySpecification(string topicName, SqlSubscriptionType subscriptionType = SqlSubscriptionType.All,
        string? routingKey = null)
        : base(topicName, subscriptionType, routingKey)
    {
    }

    public QueueSubscriptionConsumeTopologySpecification(Topic topic, SqlSubscriptionType subscriptionType = SqlSubscriptionType.All,
        string? routingKey = null)
        : base(topic.TopicName, subscriptionType, routingKey)
    {
    }

    public IEnumerable<ValidationResult> Validate()
    {
        yield break;
    }

    public void Apply(IReceiveEndpointBrokerTopologyBuilder builder)
    {
        var topicHandle = builder.CreateTopic(TopicName);

        builder.CreateQueueSubscription(topicHandle, builder.Queue, SubscriptionType, RoutingKey);
    }
}
