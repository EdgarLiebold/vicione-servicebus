using System;
using System.Diagnostics.CodeAnalysis;
using ViciOne.ServiceBus.ActiveMq.Configuration;
using ViciOne.ServiceBus.Topology;

namespace ViciOne.ServiceBus.ActiveMq.Topology;

/// <summary>Defines the ActiveMQ publish topic for one message type.</summary>
/// <typeparam name="TMessage">The published message type.</typeparam>
public class ActiveMqMessagePublishTopology<TMessage> :
    MessagePublishTopology<TMessage>,
    IActiveMqMessagePublishTopologyConfigurator<TMessage>
    where TMessage : class
{
    readonly ActiveMqTopicConfigurator _topic;

    /// <summary>Creates message publish topology using the configured virtual-topic prefix.</summary>
    /// <param name="publishTopology">The owning ActiveMQ publish topology.</param>
    /// <param name="messageTopology">The message topology supplying the entity name.</param>
    public ActiveMqMessagePublishTopology(IActiveMqPublishTopology publishTopology, IMessageTopology<TMessage> messageTopology)
        : base(publishTopology)
    {
        var topicName = $"{publishTopology.VirtualTopicPrefix}{messageTopology.EntityName}";

        var temporary = MessageTypeCache<TMessage>.IsTemporaryMessageType;

        var durable = !temporary;
        var autoDelete = temporary;

        _topic = new ActiveMqTopicConfigurator(topicName, durable, autoDelete);
    }

    /// <summary>Gets the configured publish topic.</summary>
    public Topic Topic => _topic;

    bool IActiveMqTopicConfigurator.Durable
    {
        set => _topic.Durable = value;
    }

    bool IActiveMqTopicConfigurator.AutoDelete
    {
        set => _topic.AutoDelete = value;
    }

    /// <summary>Builds the message's publish-topic address.</summary>
    /// <param name="baseAddress">The configured broker address.</param>
    /// <param name="publishAddress">The resulting absolute topic address.</param>
    /// <returns>Always <see langword="true" />.</returns>
    public override bool TryGetPublishAddress(Uri baseAddress, [NotNullWhen(true)] out Uri? publishAddress)
    {
        publishAddress = _topic.GetEndpointAddress(baseAddress);
        return true;
    }

    /// <summary>Adds the publish topic to a broker topology unless publishing is excluded.</summary>
    /// <param name="builder">The publish-topology builder.</param>
    public void Apply(IPublishEndpointBrokerTopologyBuilder builder)
    {
        if (Exclude)
            return;

        builder.Topic = builder.CreateTopic(_topic.EntityName, _topic.Durable, _topic.AutoDelete);

    }

    /// <summary>Creates send settings for the message's publish topic.</summary>
    /// <param name="hostAddress">The configured broker address.</param>
    /// <returns>The topic send settings.</returns>
    public SendSettings GetSendSettings(Uri hostAddress)
    {
        return new ActiveMqTopicSendSettings(_topic.GetEndpointAddress(hostAddress));
    }

    /// <summary>Builds the broker topology required to publish this message type.</summary>
    /// <param name="options">Options controlling the publish-topology layout.</param>
    /// <returns>The message's publish broker topology.</returns>
    public BrokerTopology GetBrokerTopology(PublishBrokerTopologyOptions options)
    {
        var builder = new PublishEndpointBrokerTopologyBuilder(options);

        Apply(builder);

        return builder.BuildBrokerTopology();
    }
}
