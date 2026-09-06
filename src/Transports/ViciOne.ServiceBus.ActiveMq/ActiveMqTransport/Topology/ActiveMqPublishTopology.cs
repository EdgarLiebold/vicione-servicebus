using System;
using ViciOne.ServiceBus.Metadata;
using ViciOne.ServiceBus.Topology;

namespace ViciOne.ServiceBus.ActiveMq.Topology;

/// <summary>Builds ActiveMQ virtual-topic publish topology for message types.</summary>
public class ActiveMqPublishTopology :
    PublishTopology,
    IActiveMqPublishTopologyConfigurator
{
    readonly IMessageTopology _messageTopology;

    /// <summary>Creates publish topology with the standard ActiveMQ virtual-topic convention.</summary>
    /// <param name="messageTopology">The message topology supplying entity names.</param>
    public ActiveMqPublishTopology(IMessageTopology messageTopology)
    {
        _messageTopology = messageTopology;

        VirtualTopicPrefix = "VirtualTopic.";
        VirtualTopicConsumerPattern = $"Consumer[.].*[.]{VirtualTopicPrefix.Replace(".", "[.]")}";
    }

    IActiveMqMessagePublishTopology<T> IActiveMqPublishTopology.GetMessageTopology<T>()
    {
        return GetMessageTopology<T>() as IActiveMqMessagePublishTopology<T>
            ?? throw new InvalidOperationException($"The publish topology for {typeof(T).FullName} is not an ActiveMQ topology.");
    }

    /// <summary>Gets or sets the prefix prepended to published message entity names.</summary>
    public string VirtualTopicPrefix { get; set; }

    /// <summary>Gets or sets the regular expression that identifies virtual-topic consumer destinations.</summary>
    public string VirtualTopicConsumerPattern { get; set; }

    IActiveMqMessagePublishTopologyConfigurator IActiveMqPublishTopologyConfigurator.GetMessageTopology(Type messageType)
    {
        return GetMessageTopology(messageType) as IActiveMqMessagePublishTopologyConfigurator
            ?? throw new InvalidOperationException($"The publish topology for {messageType.FullName} is not an ActiveMQ topology.");
    }

    /// <summary>Builds the combined broker topology for all configured publish message types.</summary>
    /// <returns>The publish broker topology.</returns>
    public BrokerTopology GetPublishBrokerTopology()
    {
        var builder = new PublishEndpointBrokerTopologyBuilder();

        ForEachMessageType<IActiveMqMessagePublishTopology>(x =>
        {
            x.Apply(builder);

            builder.Topic = null;
        });

        return builder.BuildBrokerTopology();
    }

    IActiveMqMessagePublishTopologyConfigurator<T> IActiveMqPublishTopologyConfigurator.GetMessageTopology<T>()
    {
        return GetMessageTopology<T>() as IActiveMqMessagePublishTopologyConfigurator<T>
            ?? throw new InvalidOperationException($"The publish topology for {typeof(T).FullName} is not an ActiveMQ topology.");
    }

    /// <summary>Creates publish topology for a message type and discovers its implemented message contracts.</summary>
    /// <typeparam name="T">The published message type.</typeparam>
    /// <returns>The new ActiveMQ message publish topology.</returns>
    protected override IMessagePublishTopologyConfigurator CreateMessageTopology<T>()
    {
        var messageTopology = new ActiveMqMessagePublishTopology<T>(this, _messageTopology.GetMessageTopology<T>());

        var connector = new ImplementedMessageTypeConnector<T>(this);

        ImplementedMessageTypeCache<T>.EnumerateImplementedTypes(connector);

        OnMessageTopologyCreated(messageTopology);

        return messageTopology;
    }


    class ImplementedMessageTypeConnector<TMessage> :
        IImplementedMessageType
        where TMessage : class
    {
        readonly IActiveMqPublishTopologyConfigurator _publishTopology;

        public ImplementedMessageTypeConnector(IActiveMqPublishTopologyConfigurator publishTopology)
        {
            _publishTopology = publishTopology;
        }

        public void ImplementsMessageType<T>(bool direct)
            where T : class
        {
            _publishTopology.GetMessageTopology<T>();
        }
    }
}
