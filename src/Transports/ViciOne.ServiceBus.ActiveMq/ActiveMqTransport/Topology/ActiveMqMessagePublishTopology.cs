using System;
using System.Diagnostics.CodeAnalysis;
using ViciOne.ServiceBus.ActiveMq.Configuration;
using ViciOne.ServiceBus.Topology;

#nullable enable
namespace ViciOne.ServiceBus.ActiveMq.Topology;

/// <summary>
/// Provides an active mq message publish topology implementation.
/// </summary>
/// <typeparam name="TMessage">The t message type.</typeparam>
public class ActiveMqMessagePublishTopology<TMessage> :
    MessagePublishTopology<TMessage>,
    IActiveMqMessagePublishTopologyConfigurator<TMessage>
    where TMessage : class
{
    readonly ActiveMqTopicConfigurator _topic;

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="publishTopology">The publish topology value.</param>
    /// <param name="messageTopology">The message topology value.</param>
    public ActiveMqMessagePublishTopology(IActiveMqPublishTopology publishTopology, IMessageTopology<TMessage> messageTopology)
        : base(publishTopology)
    {
        var topicName = $"{publishTopology.VirtualTopicPrefix}{messageTopology.EntityName}";

        var temporary = MessageTypeCache<TMessage>.IsTemporaryMessageType;

        var durable = !temporary;
        var autoDelete = temporary;

        _topic = new ActiveMqTopicConfigurator(topicName, durable, autoDelete);
    }

    /// <summary>
    /// Gets the topic value.
    /// </summary>
    public Topic Topic => _topic;

    bool IActiveMqTopicConfigurator.Durable
    {
        set => _topic.Durable = value;
    }

    bool IActiveMqTopicConfigurator.AutoDelete
    {
        set => _topic.AutoDelete = value;
    }

    /// <summary>
    /// Attempts to get publish address.
    /// </summary>
    /// <param name="baseAddress">The base address value.</param>
    /// <param name="publishAddress">The publish address value.</param>
    /// <returns><see langword="true" /> when the condition is satisfied; otherwise, <see langword="false" />.</returns>
    public override bool TryGetPublishAddress(Uri baseAddress, [NotNullWhen(true)] out Uri? publishAddress)
    {
        publishAddress = _topic.GetEndpointAddress(baseAddress);
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

        builder.Topic = builder.CreateTopic(_topic.EntityName, _topic.Durable, _topic.AutoDelete);

        // this was disabled previously, so not sure if it can be added
        // foreach (IActiveMqMessagePublishTopology configurator in _implementedMessageTypes)
        //     configurator.Apply(builder);
    }

    /// <summary>
    /// Gets send settings.
    /// </summary>
    /// <param name="hostAddress">The host address value.</param>
    /// <returns>The result of the operation.</returns>
    public SendSettings GetSendSettings(Uri hostAddress)
    {
        return new ActiveMqTopicSendSettings(_topic.GetEndpointAddress(hostAddress));
    }

    /// <summary>
    /// Gets broker topology.
    /// </summary>
    /// <param name="options">The options value.</param>
    /// <returns>The result of the operation.</returns>
    public BrokerTopology GetBrokerTopology(PublishBrokerTopologyOptions options)
    {
        var builder = new PublishEndpointBrokerTopologyBuilder(options);

        Apply(builder);

        return builder.BuildBrokerTopology();
    }
}
