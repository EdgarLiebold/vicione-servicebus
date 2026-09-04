using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using ViciOne.ServiceBus.SqlTransport.Configuration;
using ViciOne.ServiceBus.Topology;

#nullable enable
namespace ViciOne.ServiceBus.SqlTransport.Topology;

/// <summary>
/// Provides a sql message publish topology implementation.
/// </summary>
/// <typeparam name="TMessage">The t message type.</typeparam>
public class SqlMessagePublishTopology<TMessage> :
    MessagePublishTopology<TMessage>,
    ISqlMessagePublishTopologyConfigurator<TMessage>
    where TMessage : class
{
    readonly List<ISqlMessagePublishTopology> _implementedMessageTypes;
    readonly SqlTopicConfigurator _topic;

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="publishTopology">The publish topology value.</param>
    /// <param name="messageTopology">The message topology value.</param>
    public SqlMessagePublishTopology(ISqlPublishTopology publishTopology, IMessageTopology<TMessage> messageTopology)
        : base(publishTopology)
    {
        var exchangeName = messageTopology.EntityName;

        _topic = new SqlTopicConfigurator(exchangeName);

        _implementedMessageTypes = new List<ISqlMessagePublishTopology>();
    }

    /// <summary>
    /// Gets the topic value.
    /// </summary>
    public Topic Topic => _topic;

    /// <summary>
    /// Applies this specification to the target builder.
    /// </summary>
    /// <param name="builder">The builder value.</param>
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
    /// Gets broker topology.
    /// </summary>
    /// <returns>The result of the operation.</returns>
    public BrokerTopology GetBrokerTopology()
    {
        var builder = new PublishEndpointBrokerTopologyBuilder();

        Apply(builder);

        return builder.BuildBrokerTopology();
    }

    /// <summary>
    /// Gets send settings.
    /// </summary>
    /// <param name="hostAddress">The host address value.</param>
    /// <returns>The result of the operation.</returns>
    public SendSettings GetSendSettings(Uri hostAddress)
    {
        return new QueueSendSettings(_topic.GetEndpointAddress(hostAddress));
    }

    /// <summary>
    /// Adds implemented message configurator to the configuration.
    /// </summary>
    /// <typeparam name="T">The t type.</typeparam>
    /// <param name="configurator">The configurator value.</param>
    /// <param name="direct">The direct value.</param>
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
