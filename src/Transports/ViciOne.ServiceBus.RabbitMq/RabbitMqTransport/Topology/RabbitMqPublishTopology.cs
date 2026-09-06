using System;
using ViciOne.ServiceBus.Metadata;
using ViciOne.ServiceBus.Topology;

namespace ViciOne.ServiceBus.RabbitMq.Topology;

/// <summary>Creates and aggregates RabbitMQ publish topology for message contracts.</summary>
public class RabbitMqPublishTopology :
    PublishTopology,
    IRabbitMqPublishTopologyConfigurator
{
    readonly IMessageTopology _messageTopology;

    /// <summary>Creates a publish topology that uses fanout exchanges by default.</summary>
    /// <param name="messageTopology">The message metadata used to derive exchange names.</param>
    public RabbitMqPublishTopology(IMessageTopology messageTopology)
    {
        _messageTopology = messageTopology;
        ExchangeTypeSelector = new FanoutExchangeTypeSelector();
    }

    /// <summary>Gets the selector used to determine exchange types for published message contracts.</summary>
    public IExchangeTypeSelector ExchangeTypeSelector { get; }

    /// <summary>Gets or sets how implemented-message exchange hierarchies are represented.</summary>
    public PublishBrokerTopologyOptions BrokerTopologyOptions { get; set; }

    IRabbitMqMessagePublishTopology<T> IRabbitMqPublishTopology.GetMessageTopology<T>()
    {
        return GetMessageTopology<T>() as IRabbitMqMessagePublishTopologyConfigurator<T>
            ?? throw new InvalidOperationException($"The message topology for '{typeof(T)}' is not a RabbitMQ publish topology.");
    }

    IRabbitMqMessagePublishTopologyConfigurator IRabbitMqPublishTopologyConfigurator.GetMessageTopology(Type messageType)
    {
        return GetMessageTopology(messageType) as IRabbitMqMessagePublishTopologyConfigurator
            ?? throw new InvalidOperationException($"The message topology for '{messageType}' is not a RabbitMQ publish topology.");
    }

    /// <summary>Builds the combined broker topology for every configured publish message contract.</summary>
    /// <returns>The de-duplicated publish broker topology.</returns>
    public BrokerTopology GetPublishBrokerTopology()
    {
        var builder = new PublishEndpointBrokerTopologyBuilder(BrokerTopologyOptions);

        ForEachMessageType<IRabbitMqMessagePublishTopology>(x =>
        {
            x.Apply(builder);

            builder.Exchange = null;
        });

        return builder.BuildBrokerTopology();
    }

    IRabbitMqMessagePublishTopologyConfigurator<T> IRabbitMqPublishTopologyConfigurator.GetMessageTopology<T>()
    {
        return GetMessageTopology<T>() as IRabbitMqMessagePublishTopologyConfigurator<T>
            ?? throw new InvalidOperationException($"The message topology for '{typeof(T)}' is not a RabbitMQ publish topology.");
    }

    /// <summary>Creates publish topology for a message contract and connects its directly implemented contracts.</summary>
    /// <typeparam name="T">The published message contract type.</typeparam>
    /// <returns>The RabbitMQ publish topology for <typeparamref name="T"/>.</returns>
    protected override IMessagePublishTopologyConfigurator CreateMessageTopology<T>()
    {
        var exchangeTypeSelector = new MessageExchangeTypeSelector<T>(ExchangeTypeSelector);

        var messageTopology = new RabbitMqMessagePublishTopology<T>(this, _messageTopology.GetMessageTopology<T>(), exchangeTypeSelector);

        var connector = new ImplementedMessageTypeConnector<T>(this, messageTopology);

        ImplementedMessageTypeCache<T>.EnumerateImplementedTypes(connector);

        OnMessageTopologyCreated(messageTopology);

        return messageTopology;
    }


    class ImplementedMessageTypeConnector<TMessage> :
        IImplementedMessageType
        where TMessage : class
    {
        readonly RabbitMqMessagePublishTopology<TMessage> _messagePublishTopologyConfigurator;
        readonly IRabbitMqPublishTopologyConfigurator _publishTopology;

        public ImplementedMessageTypeConnector(IRabbitMqPublishTopologyConfigurator publishTopology,
            RabbitMqMessagePublishTopology<TMessage> messagePublishTopologyConfigurator)
        {
            _publishTopology = publishTopology;
            _messagePublishTopologyConfigurator = messagePublishTopologyConfigurator;
        }

        public void ImplementsMessageType<T>(bool direct)
            where T : class
        {
            IRabbitMqMessagePublishTopologyConfigurator<T> messageTopology = _publishTopology.GetMessageTopology<T>();

            _messagePublishTopologyConfigurator.AddImplementedMessageConfigurator(messageTopology, direct);
        }
    }
}
