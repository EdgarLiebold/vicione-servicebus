using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using ViciOne.ServiceBus.AmazonSqs.Configuration;
using ViciOne.ServiceBus.Internals;
using ViciOne.ServiceBus.Topology;

namespace ViciOne.ServiceBus.AmazonSqs.Topology;

/// <summary>
/// Provides an amazon sqs message publish topology implementation.
/// </summary>
/// <typeparam name="TMessage">The t message type.</typeparam>
public class AmazonSqsMessagePublishTopology<TMessage> :
    MessagePublishTopology<TMessage>,
    IAmazonSqsMessagePublishTopologyConfigurator<TMessage>
    where TMessage : class
{
    readonly AmazonSqsTopicConfigurator _amazonSqsTopic;
    readonly IAmazonSqsPublishTopology _publishTopology;

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="publishTopology">The publish topology value.</param>
    /// <param name="messageTopology">The message topology value.</param>
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

    /// <summary>
    /// Gets the topic value.
    /// </summary>
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

    /// <summary>
    /// Gets endpoint address.
    /// </summary>
    /// <param name="hostAddress">The host address value.</param>
    /// <returns>The result of the operation.</returns>
    public AmazonSqsEndpointAddress GetEndpointAddress(Uri hostAddress)
    {
        return _amazonSqsTopic.GetEndpointAddress(hostAddress);
    }

    /// <summary>
    /// Attempts to get publish address.
    /// </summary>
    /// <param name="baseAddress">The base address value.</param>
    /// <param name="publishAddress">The publish address value.</param>
    /// <returns><see langword="true" /> when the condition is satisfied; otherwise, <see langword="false" />.</returns>
    public override bool TryGetPublishAddress(Uri baseAddress, [NotNullWhen(true)] out Uri? publishAddress)
    {
        publishAddress = _amazonSqsTopic.GetEndpointAddress(baseAddress);
        return true;
    }

    /// <summary>
    /// Applies this specification to the target builder.
    /// </summary>
    /// <param name="builder">The builder value.</param>
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

    /// <summary>
    /// Gets publish settings.
    /// </summary>
    /// <param name="hostAddress">The host address value.</param>
    /// <returns>The result of the operation.</returns>
    public PublishSettings GetPublishSettings(Uri hostAddress)
    {
        return new TopicPublishSettings(GetEndpointAddress(hostAddress));
    }

    /// <summary>
    /// Gets broker topology.
    /// </summary>
    /// <returns>The result of the operation.</returns>
    public BrokerTopology GetBrokerTopology()
    {
        var builder = new PublishEndpointBrokerTopologyBuilder();

        Apply(builder);

        return builder.BuildBrokerTopology();
    }
}
