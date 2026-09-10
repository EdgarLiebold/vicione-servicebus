using System.Collections.Generic;
using ViciOne.ServiceBus.SqlTransport.Topology;

namespace ViciOne.ServiceBus.SqlTransport.Configuration;

/// <summary>Configures a topic subscription for published messages.</summary>
public class TopicSubscriptionPublishTopologySpecification :
    SqlTopicSubscriptionConfigurator,
    ISqlPublishTopologySpecification
{
    /// <summary>Initializes a new instance.</summary>
    /// <param name="topicName">The topic name.</param>
    /// <param name="subscriptionType">The runtime subscription type used by the operation.</param>
    /// <param name="routingKey">The routing key.</param>
    public TopicSubscriptionPublishTopologySpecification(string topicName, SqlSubscriptionType subscriptionType = SqlSubscriptionType.All,
        string? routingKey = null)
        : base(topicName, subscriptionType, routingKey)
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
    public void Apply(IPublishEndpointBrokerTopologyBuilder builder)
    {
        var exchangeHandle = builder.CreateTopic(TopicName);

        if (builder.Topic != null)
            builder.CreateTopicSubscription(builder.Topic, exchangeHandle, SubscriptionType, RoutingKey);
    }
}
