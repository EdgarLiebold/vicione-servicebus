using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using ViciOne.ServiceBus.SqlTransport.Configuration;
using ViciOne.ServiceBus.Topology;

namespace ViciOne.ServiceBus.SqlTransport.Topology;

/// <summary>Defines the topology for sql message publish.</summary>
/// <typeparam name="TMessage">The message contract processed by the member.</typeparam>
public class SqlMessagePublishTopology<TMessage> :
    MessagePublishTopology<TMessage>,
    ISqlMessagePublishTopologyConfigurator<TMessage>
    where TMessage : class
{
    readonly List<ISqlMessagePublishTopology> _implementedMessageTypes;
    readonly SqlTopicConfigurator _topic;

    /// <summary>Initializes a new instance.</summary>
    /// <param name="publishTopology">The publish topology.</param>
    /// <param name="messageTopology">The message topology.</param>
    public SqlMessagePublishTopology(ISqlPublishTopology publishTopology, IMessageTopology<TMessage> messageTopology)
        : base(publishTopology)
    {
        var exchangeName = messageTopology.EntityName;

        _topic = new SqlTopicConfigurator(exchangeName);

        _implementedMessageTypes = new List<ISqlMessagePublishTopology>();
    }

    /// <summary>Gets the topic.</summary>
    public Topic Topic => _topic;

    /// <summary>Applies this specification to the target builder.</summary>
    /// <param name="builder">The builder that receives the configuration.</param>
    public void Apply(IPublishEndpointBrokerTopologyBuilder builder)
    {
        if (Exclude)
            return;

        var topicHandle = builder.CreateTopic(_topic.TopicName);

        if (builder.Topic != null)
            builder.CreateTopicSubscription(builder.Topic, topicHandle);
        else
            builder.Topic = topicHandle;

        foreach (var configurator in _implementedMessageTypes)
            configurator.Apply(builder);
    }

    /// <summary>Attempts to get publish address.</summary>
    /// <param name="baseAddress">The base address.</param>
    /// <param name="publishAddress">Receives the publish address produced by the operation.</param>
    /// <returns><see langword="true" /> when the condition is satisfied; otherwise, <see langword="false" />.</returns>
    public override bool TryGetPublishAddress(Uri baseAddress, [NotNullWhen(true)] out Uri? publishAddress)
    {
        publishAddress = _topic.GetEndpointAddress(baseAddress);
        return true;
    }

    /// <summary>Gets broker topology.</summary>
    /// <returns>The broker topology.</returns>
    public BrokerTopology GetBrokerTopology()
    {
        var builder = new PublishEndpointBrokerTopologyBuilder();

        Apply(builder);

        return builder.BuildBrokerTopology();
    }

    /// <summary>Gets send settings.</summary>
    /// <param name="hostAddress">The host address.</param>
    /// <returns>The send settings.</returns>
    public SendSettings GetSendSettings(Uri hostAddress)
    {
        return new QueueSendSettings(_topic.GetEndpointAddress(hostAddress));
    }

    /// <summary>Adds implemented message configurator to the configuration.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="configurator">The configurator to update.</param>
    /// <param name="direct">The direct.</param>
    public void AddImplementedMessageConfigurator<T>(ISqlMessagePublishTopologyConfigurator<T> configurator, bool direct)
        where T : class
    {
        var adapter = new ImplementedTypeAdapter<T>(configurator, direct);

        _implementedMessageTypes.Add(adapter);
    }


    class ImplementedTypeAdapter<T> :
        ISqlMessagePublishTopology
        where T : class
    {
        readonly ISqlMessagePublishTopologyConfigurator<T> _configurator;
        readonly bool _direct;

        public ImplementedTypeAdapter(ISqlMessagePublishTopologyConfigurator<T> configurator, bool direct)
        {
            _configurator = configurator;
            _direct = direct;
        }

        public void Apply(IPublishEndpointBrokerTopologyBuilder builder)
        {
            if (_direct)
            {
                var implementedBuilder = builder.CreateImplementedBuilder();

                _configurator.Apply(implementedBuilder);
            }
        }
    }
}
