using System;
using System.Collections.Generic;
using ViciOne.ServiceBus.Topology;

namespace ViciOne.ServiceBus.AmazonSqs.Topology;

/// <summary>Configures Amazon SNS topics for published message types.</summary>
public class AmazonSqsPublishTopology :
    PublishTopology,
    IAmazonSqsPublishTopologyConfigurator
{
    readonly IMessageTopology _messageTopology;

    /// <summary>Initializes Amazon SNS publish topology.</summary>
    /// <param name="messageTopology">The message-topology convention source.</param>
    public AmazonSqsPublishTopology(IMessageTopology messageTopology)
    {
        _messageTopology = messageTopology;

        TopicAttributes = new Dictionary<string, object>();
        TopicSubscriptionAttributes = new Dictionary<string, object>();
        TopicTags = new Dictionary<string, string>();
    }

    /// <summary>Gets the default Amazon SNS topic attributes.</summary>
    public IDictionary<string, object> TopicAttributes { get; private set; }
    /// <summary>Gets the default Amazon SNS subscription attributes.</summary>
    public IDictionary<string, object> TopicSubscriptionAttributes { get; private set; }
    /// <summary>Gets the default tags applied to Amazon SNS topics.</summary>
    public IDictionary<string, string> TopicTags { get; private set; }

    IAmazonSqsMessagePublishTopology<T> IAmazonSqsPublishTopology.GetMessageTopology<T>()
    {
        return (GetMessageTopology<T>() as IAmazonSqsMessagePublishTopology<T>)!;
    }

    IAmazonSqsMessagePublishTopologyConfigurator IAmazonSqsPublishTopologyConfigurator.GetMessageTopology(Type messageType)
    {
        return (GetMessageTopology(messageType) as IAmazonSqsMessagePublishTopologyConfigurator)!;
    }

    /// <summary>Builds the combined broker topology for all configured message publish types.</summary>
    /// <returns>The aggregate Amazon SNS publish topology.</returns>
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

    /// <summary>Creates and announces Amazon SNS publish topology for a message type.</summary>
    /// <typeparam name="T">The published message type.</typeparam>
    /// <returns>The message publish-topology configurator.</returns>
    protected override IMessagePublishTopologyConfigurator CreateMessageTopology<T>()
    {
        var messageTopology = new AmazonSqsMessagePublishTopology<T>(this, _messageTopology.GetMessageTopology<T>());

        OnMessageTopologyCreated(messageTopology);

        return messageTopology;
    }
}
