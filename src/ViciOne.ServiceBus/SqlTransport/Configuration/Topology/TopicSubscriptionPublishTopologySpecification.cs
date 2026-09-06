using System.Collections.Generic;
using ViciOne.ServiceBus.SqlTransport.Topology;

namespace ViciOne.ServiceBus.SqlTransport.Configuration;

/// <summary>
/// Used to bind an exchange to the sending
/// </summary>
public class TopicSubscriptionPublishTopologySpecification :
    SqlTopicSubscriptionConfigurator,
    ISqlPublishTopologySpecification
{
    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="topicName">The topic name value.</param>
    /// <param name="subscriptionType">The subscription type value.</param>
    /// <param name="routingKey">The routing key value.</param>
    public TopicSubscriptionPublishTopologySpecification(string topicName, SqlSubscriptionType subscriptionType = SqlSubscriptionType.All,
        string? routingKey = null)
        : base(topicName, subscriptionType, routingKey)
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
    public void Apply(IPublishEndpointBrokerTopologyBuilder builder)
    {
        var exchangeHandle = builder.CreateTopic(TopicName);

        if (builder.Topic != null)
            builder.CreateTopicSubscription(builder.Topic, exchangeHandle, SubscriptionType, RoutingKey);
    }
}
