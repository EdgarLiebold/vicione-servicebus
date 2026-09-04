using System.Collections.Generic;
using ViciOne.ServiceBus.SqlTransport.Topology;

#nullable enable
namespace ViciOne.ServiceBus.SqlTransport.Configuration;

/// <summary>
/// Provides a queue subscription consume topology specification implementation.
/// </summary>
public class QueueSubscriptionConsumeTopologySpecification :
    SqlTopicSubscriptionConfigurator,
    ISqlConsumeTopologySpecification
{
    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="topicName">The topic name value.</param>
    /// <param name="subscriptionType">The subscription type value.</param>
    /// <param name="routingKey">The routing key value.</param>
    public QueueSubscriptionConsumeTopologySpecification(string topicName, SqlSubscriptionType subscriptionType = SqlSubscriptionType.All,
        string? routingKey = null)
        : base(topicName, subscriptionType, routingKey)
    {
    }

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="topic">The topic value.</param>
    /// <param name="subscriptionType">The subscription type value.</param>
    /// <param name="routingKey">The routing key value.</param>
    public QueueSubscriptionConsumeTopologySpecification(Topic topic, SqlSubscriptionType subscriptionType = SqlSubscriptionType.All,
        string? routingKey = null)
        : base(topic.TopicName, subscriptionType, routingKey)
    {
    }

    /// <summary>
    /// Validates the current configuration.
    /// </summary>
    /// <returns>The result of the operation.</returns>
    public IEnumerable<ValidationResult> Validate()
    {
        yield break;
    }

    /// <summary>
    /// Applies this specification to the target builder.
    /// </summary>
    /// <param name="builder">The builder value.</param>
    public void Apply(IReceiveEndpointBrokerTopologyBuilder builder)
    {
        var topicHandle = builder.CreateTopic(TopicName);

        builder.CreateQueueSubscription(topicHandle, builder.Queue, SubscriptionType, RoutingKey);
    }
}
