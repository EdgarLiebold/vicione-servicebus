using System.Collections.Generic;
using ViciOne.ServiceBus.AmazonSqs.Topology;
using ViciOne.ServiceBus.Internals;

namespace ViciOne.ServiceBus.AmazonSqs.Configuration;
/// <summary>Declares an Amazon SNS topic subscription that targets the receive endpoint's Amazon SQS queue.</summary>
public class ConsumerConsumeTopologySpecification :
    AmazonSqsTopicSubscriptionConfigurator,
    IAmazonSqsConsumeTopologySpecification
{
    readonly IAmazonSqsPublishTopology _publishTopology;

    /// <summary>Initializes a consume-topology specification for a named topic.</summary>
    /// <param name="publishTopology">The publish conventions and attributes merged into the topic.</param>
    /// <param name="topicName">The Amazon SNS topic name.</param>
    /// <param name="durable">Whether the topic is retained when the endpoint stops.</param>
    /// <param name="autoDelete">Whether the topic is deleted when the endpoint stops.</param>
    public ConsumerConsumeTopologySpecification(IAmazonSqsPublishTopology publishTopology, string topicName, bool durable = true, bool autoDelete = false)
        : base(topicName, durable, autoDelete)
    {
        _publishTopology = publishTopology;
    }

    /// <summary>Initializes a consume-topology specification from an existing topic entity.</summary>
    /// <param name="publishTopology">The publish conventions and attributes merged into the topic.</param>
    /// <param name="topic">The topic topology entity.</param>
    public ConsumerConsumeTopologySpecification(IAmazonSqsPublishTopology publishTopology, Topic topic)
        : base(topic)
    {
        _publishTopology = publishTopology;
    }

    /// <summary>Returns no additional validation failures.</summary>
    /// <returns>An empty validation sequence.</returns>
    public IEnumerable<ValidationResult> Validate()
    {
        return [];
    }

    /// <summary>Creates the merged topic and subscribes the builder's receive queue when one is present.</summary>
    /// <param name="builder">The receive-endpoint broker-topology builder.</param>
    public void Apply(IReceiveEndpointBrokerTopologyBuilder builder)
    {
        var topicHandle = builder.CreateTopic(EntityName, Durable, AutoDelete,
            _publishTopology.TopicAttributes.MergeLeft(TopicAttributes),
            _publishTopology.TopicSubscriptionAttributes.MergeLeft(TopicSubscriptionAttributes),
            _publishTopology.TopicTags.MergeLeft(Tags));


        if (builder.Queue != null)
            builder.CreateQueueSubscription(topicHandle, builder.Queue);
    }
}
