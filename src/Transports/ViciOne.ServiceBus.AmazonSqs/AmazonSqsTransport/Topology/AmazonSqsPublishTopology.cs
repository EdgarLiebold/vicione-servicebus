using System;
using System.Collections.Generic;
using ViciOne.ServiceBus.Topology;

namespace ViciOne.ServiceBus.AmazonSqs.Topology;

/// <summary>
/// Provides an amazon sqs publish topology implementation.
/// </summary>
public class AmazonSqsPublishTopology :
    PublishTopology,
    IAmazonSqsPublishTopologyConfigurator
{
    readonly IMessageTopology _messageTopology;

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="messageTopology">The message topology value.</param>
    public AmazonSqsPublishTopology(IMessageTopology messageTopology)
    {
        _messageTopology = messageTopology;

        TopicAttributes = new Dictionary<string, object>();
        TopicSubscriptionAttributes = new Dictionary<string, object>();
        TopicTags = new Dictionary<string, string>();
    }

    /// <summary>
    /// Gets or sets the topic attributes value.
    /// </summary>
    public IDictionary<string, object> TopicAttributes { get; private set; }
    /// <summary>
    /// Gets or sets the topic subscription attributes value.
    /// </summary>
    public IDictionary<string, object> TopicSubscriptionAttributes { get; private set; }
    /// <summary>
    /// Gets or sets the topic tags value.
    /// </summary>
    public IDictionary<string, string> TopicTags { get; private set; }

    IAmazonSqsMessagePublishTopology<T> IAmazonSqsPublishTopology.GetMessageTopology<T>()
    {
        return (GetMessageTopology<T>() as IAmazonSqsMessagePublishTopology<T>)!;
    }

    IAmazonSqsMessagePublishTopologyConfigurator IAmazonSqsPublishTopologyConfigurator.GetMessageTopology(Type messageType)
    {
        return (GetMessageTopology(messageType) as IAmazonSqsMessagePublishTopologyConfigurator)!;
    }

    /// <summary>
    /// Gets publish broker topology.
    /// </summary>
    /// <returns>The result of the operation.</returns>
    public BrokerTopology GetPublishBrokerTopology()
    {
        var builder = new PublishEndpointBrokerTopologyBuilder();

        ForEachMessageType<IAmazonSqsMessagePublishTopology>(x =>
        {
            x.Apply(builder);

            builder.Topic = null;
        });

        return builder.BuildBrokerTopology();
    }

    IAmazonSqsMessagePublishTopologyConfigurator<T> IAmazonSqsPublishTopologyConfigurator.GetMessageTopology<T>()
    {
        return (GetMessageTopology<T>() as IAmazonSqsMessagePublishTopologyConfigurator<T>)!;
    }

    /// <summary>
    /// Creates message topology.
    /// </summary>
    /// <typeparam name="T">The t type.</typeparam>
    /// <returns>The result of the operation.</returns>
    protected override IMessagePublishTopologyConfigurator CreateMessageTopology<T>()
    {
        var messageTopology = new AmazonSqsMessagePublishTopology<T>(this, _messageTopology.GetMessageTopology<T>());

        OnMessageTopologyCreated(messageTopology);

        return messageTopology;
    }
}
