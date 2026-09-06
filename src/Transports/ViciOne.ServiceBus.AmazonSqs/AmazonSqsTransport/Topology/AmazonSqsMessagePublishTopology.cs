using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using ViciOne.ServiceBus.AmazonSqs.Configuration;
using ViciOne.ServiceBus.Internals;
using ViciOne.ServiceBus.Topology;

namespace ViciOne.ServiceBus.AmazonSqs.Topology;

/// <summary>Configures the Amazon SNS topic used to publish a message type.</summary>
/// <typeparam name="TMessage">The message contract processed by the member.</typeparam>
public class AmazonSqsMessagePublishTopology<TMessage> :
    MessagePublishTopology<TMessage>,
    IAmazonSqsMessagePublishTopologyConfigurator<TMessage>
    where TMessage : class
{
    readonly AmazonSqsTopicConfigurator _amazonSqsTopic;
    readonly IAmazonSqsPublishTopology _publishTopology;

    /// <summary>Initializes publish topology using the message entity name and temporary-type lifetime.</summary>
    /// <param name="publishTopology">The parent Amazon SNS publish topology.</param>
    /// <param name="messageTopology">The message topology that supplies the topic name.</param>
    public AmazonSqsMessagePublishTopology(IAmazonSqsPublishTopology publishTopology, IMessageTopology<TMessage> messageTopology)
        : base(publishTopology)
    {
        _publishTopology = publishTopology;

        var topicName = messageTopology.EntityName;

        var temporary = MessageTypeCache<TMessage>.IsTemporaryMessageType;

        var durable = !temporary;
        var autoDelete = temporary;

        _amazonSqsTopic = new AmazonSqsTopicConfigurator(topicName, durable, autoDelete);
    }

    /// <summary>Gets the configured Amazon SNS topic entity.</summary>
    public Topic Topic => _amazonSqsTopic;

    bool IAmazonSqsTopicConfigurator.Durable
    {
        set => _amazonSqsTopic.Durable = value;
    }

    bool IAmazonSqsTopicConfigurator.AutoDelete
    {
        set => _amazonSqsTopic.AutoDelete = value;
    }

    IDictionary<string, object> IAmazonSqsTopicConfigurator.TopicAttributes => _amazonSqsTopic.TopicAttributes;
    IDictionary<string, object> IAmazonSqsTopicConfigurator.TopicSubscriptionAttributes => _amazonSqsTopic.TopicSubscriptionAttributes;
    IDictionary<string, string> IAmazonSqsTopicConfigurator.TopicTags => _amazonSqsTopic.TopicTags;

    /// <summary>Formats the topic endpoint address relative to an Amazon SQS host.</summary>
    /// <param name="hostAddress">The Amazon SQS host address.</param>
    /// <returns>The Amazon SNS topic endpoint address.</returns>
    public AmazonSqsEndpointAddress GetEndpointAddress(Uri hostAddress)
    {
        return _amazonSqsTopic.GetEndpointAddress(hostAddress);
    }

    /// <summary>Gets the message type's Amazon SNS publish address.</summary>
    /// <param name="baseAddress">The Amazon SQS host address.</param>
    /// <param name="publishAddress">The formatted topic address.</param>
    /// <returns>Always <see langword="true"/>.</returns>
    public override bool TryGetPublishAddress(Uri baseAddress, [NotNullWhen(true)] out Uri? publishAddress)
    {
        publishAddress = _amazonSqsTopic.GetEndpointAddress(baseAddress);
        return true;
    }

    /// <summary>Adds the message topic to a publish builder unless the topology is excluded.</summary>
    /// <param name="builder">The publish-endpoint broker-topology builder.</param>
    public void Apply(IPublishEndpointBrokerTopologyBuilder builder)
    {
        if (Exclude)
            return;

        var topicHandle = builder.CreateTopic(_amazonSqsTopic.EntityName, _amazonSqsTopic.Durable, _amazonSqsTopic.AutoDelete,
            _publishTopology.TopicAttributes.MergeLeft(_amazonSqsTopic.TopicAttributes),
            _publishTopology.TopicSubscriptionAttributes.MergeLeft(_amazonSqsTopic.TopicSubscriptionAttributes),
            _publishTopology.TopicTags.MergeLeft(_amazonSqsTopic.Tags));

        builder.Topic ??= topicHandle;
    }

    /// <summary>Creates publish settings for the message topic.</summary>
    /// <param name="hostAddress">The Amazon SQS host address.</param>
    /// <returns>The Amazon SNS topic publish settings.</returns>
    public PublishSettings GetPublishSettings(Uri hostAddress)
    {
        return new TopicPublishSettings(GetEndpointAddress(hostAddress));
    }

    /// <summary>Builds broker topology for this message topic.</summary>
    /// <returns>The topic broker topology.</returns>
    public BrokerTopology GetBrokerTopology()
    {
        var builder = new PublishEndpointBrokerTopologyBuilder();

        Apply(builder);

        return builder.BuildBrokerTopology();
    }
}
