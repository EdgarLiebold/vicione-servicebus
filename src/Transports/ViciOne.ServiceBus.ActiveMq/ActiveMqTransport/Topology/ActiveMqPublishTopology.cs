using System;
using ViciOne.ServiceBus.Metadata;
using ViciOne.ServiceBus.Topology;

namespace ViciOne.ServiceBus.ActiveMq.Topology;

/// <summary>
/// Provides an active mq publish topology implementation.
/// </summary>
public class ActiveMqPublishTopology :
    PublishTopology,
    IActiveMqPublishTopologyConfigurator
{
    readonly IMessageTopology _messageTopology;

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="messageTopology">The message topology value.</param>
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

    /// <summary>
    /// Gets or sets the virtual topic prefix value.
    /// </summary>
    public string VirtualTopicPrefix { get; set; }

    /// <summary>
    /// Gets or sets the virtual topic consumer pattern value.
    /// </summary>
    public string VirtualTopicConsumerPattern { get; set; }

    IActiveMqMessagePublishTopologyConfigurator IActiveMqPublishTopologyConfigurator.GetMessageTopology(Type messageType)
    {
        return GetMessageTopology(messageType) as IActiveMqMessagePublishTopologyConfigurator
            ?? throw new InvalidOperationException($"The publish topology for {messageType.FullName} is not an ActiveMQ topology.");
    }

    /// <summary>
    /// Gets publish broker topology.
    /// </summary>
    /// <returns>The result of the operation.</returns>
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

    /// <summary>
    /// Creates message topology.
    /// </summary>
    /// <typeparam name="T">The t type.</typeparam>
    /// <returns>The result of the operation.</returns>
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
