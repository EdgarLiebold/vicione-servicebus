using System.Collections.Generic;
using ViciOne.ServiceBus.AmazonSqs.Topology;
using ViciOne.ServiceBus.Internals;

namespace ViciOne.ServiceBus.AmazonSqs.Configuration;
/// <summary>
/// Used to by a TopicSubscription destination to the receive endpoint, via an additional message consumer
/// </summary>
public class ConsumerConsumeTopologySpecification :
    AmazonSqsTopicSubscriptionConfigurator,
    IAmazonSqsConsumeTopologySpecification
{
    readonly IAmazonSqsPublishTopology _publishTopology;

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="publishTopology">The publish topology value.</param>
    /// <param name="topicName">The topic name value.</param>
    /// <param name="durable">The durable value.</param>
    /// <param name="autoDelete">The auto delete value.</param>
    public ConsumerConsumeTopologySpecification(IAmazonSqsPublishTopology publishTopology, string topicName, bool durable = true, bool autoDelete = false)
        : base(topicName, durable, autoDelete)
    {
        _publishTopology = publishTopology;
    }

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="publishTopology">The publish topology value.</param>
    /// <param name="topic">The topic value.</param>
    public ConsumerConsumeTopologySpecification(IAmazonSqsPublishTopology publishTopology, Topic topic)
        : base(topic)
    {
        _publishTopology = publishTopology;
    }

    /// <summary>
    /// Validates the current configuration.
    /// </summary>
    /// <returns>The result of the operation.</returns>
    public IEnumerable<ValidationResult> Validate()
    {
        return [];
    }

    /// <summary>
    /// Applies this specification to the target builder.
    /// </summary>
    /// <param name="builder">The builder value.</param>
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
