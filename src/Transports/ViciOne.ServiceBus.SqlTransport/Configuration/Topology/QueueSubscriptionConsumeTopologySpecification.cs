using System.Collections.Generic;
using ViciOne.ServiceBus.SqlTransport.Topology;

namespace ViciOne.ServiceBus.SqlTransport.Configuration;

/// <summary>Describes requirements for queue subscription consume topology.</summary>
public class QueueSubscriptionConsumeTopologySpecification :
    SqlTopicSubscriptionConfigurator,
    ISqlConsumeTopologySpecification
{
    /// <summary>Initializes a new instance.</summary>
    /// <param name="topicName">The topic name.</param>
    /// <param name="subscriptionType">The runtime subscription type used by the operation.</param>
    /// <param name="routingKey">The routing key.</param>
    public QueueSubscriptionConsumeTopologySpecification(string topicName, SqlSubscriptionType subscriptionType = SqlSubscriptionType.All,
        string? routingKey = null)
        : base(topicName, subscriptionType, routingKey)
    {
    }

    /// <summary>Initializes a new instance.</summary>
    /// <param name="topic">The topic.</param>
    /// <param name="subscriptionType">The runtime subscription type used by the operation.</param>
    /// <param name="routingKey">The routing key.</param>
    public QueueSubscriptionConsumeTopologySpecification(Topic topic, SqlSubscriptionType subscriptionType = SqlSubscriptionType.All,
        string? routingKey = null)
        : base(topic.TopicName, subscriptionType, routingKey)
    {
    }

    /// <summary>Validates the current configuration.</summary>
    /// <returns>The validation failures.</returns>
    public IEnumerable<ValidationResult> Validate()
    {
        yield break;
    }

    /// <summary>Applies this specification to the target builder.</summary>
    /// <param name="builder">The builder that receives the configuration.</param>
    public void Apply(IReceiveEndpointBrokerTopologyBuilder builder)
    {
        var topicHandle = builder.CreateTopic(TopicName);

        builder.CreateQueueSubscription(topicHandle, builder.Queue, SubscriptionType, RoutingKey);
    }
}
